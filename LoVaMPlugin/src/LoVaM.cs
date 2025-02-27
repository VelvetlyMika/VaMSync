using System.Collections.Generic;
using LoVaMPlugin.MotionSources;
using LoVaMPlugin.Network;
using UnityEngine;

namespace LoVaMPlugin
{
    public class LoVaM : MVRScript
    {
        private static LoVaM _instance;
        
        private const string ServerIP = "192.168.178.73";
        private const int ServerListenPort = 20010;
        private const float NetworkListenInterval = 0.033f;
        
        private INetwork _network;
        private float _networkPollTimer;

        private byte _lastSentLaunchPos;

        private JSONStorableStringChooser _motionSourceChooser;
        private JSONStorableBool _pauseLaunchMessages;
        private JSONStorableFloat _simulatorPosition;
        
        private float _simulatorTarget;
        private float _simulatorSpeed;

        private IMotionSource _currentMotionSource;
        private int _currentMotionSourceIndex = -1;
        private int _desiredMotionSourceIndex;
        
        private readonly List<string> _motionSourceChoices = new List<string>
        {
            "Oscillate",
            "Pattern",
            "Zone"
        };

        private readonly List<IMotionSource> _motionSources = new List<IMotionSource>
        {
            new OscillateSource(),
            new PatternSource(),
            new ZoneSource()
        };
        
        public override void Init()
        {
            if (_instance != null)
            {
                SuperController.LogError("You can only have one instance of VAM Launch active!");
                return;
            }
            
            if (containingAtom == null || containingAtom.type == "CoreControl")
            {
                SuperController.LogError("Please add VAM Launch to in scene atom!");
                return;
            }

            _instance = this;

            InitPluginSettings();
            InitOptionsUI();
            InitStorableActions();
            InitNetwork();
        }

        private void InitNetwork()
        {
            StopNetwork();

            _network = new LoVaMNetwork();
            _network.Init(ServerIP, ServerListenPort);
            SuperController.LogMessage("LoVaM connection to Lovense remote established.");
            _network.Send("{ \"command\": \"GetToys\" }");
        }
        
        private void InitPluginSettings()
        {
            _motionSourceChooser = new JSONStorableStringChooser(
                "motionSource",
                _motionSourceChoices, 
                "",
                "Motion Source",
                (srcName) => {
                    _desiredMotionSourceIndex = GetMotionSourceIndex(srcName);
                }
            )
            {
                choices = _motionSourceChoices
            };
            RegisterStringChooser(_motionSourceChooser);
            if (string.IsNullOrEmpty(_motionSourceChooser.val))
            {
                _motionSourceChooser.SetVal(_motionSourceChoices[0]);
            }
            
            _pauseLaunchMessages = new JSONStorableBool("pauseLaunchMessages", true);
            RegisterBool(_pauseLaunchMessages);
            
            _simulatorPosition = new JSONStorableFloat("simulatorPosition", 0.0f, 0.0f, LaunchUtils.LAUNCH_MAX_VAL);
            RegisterFloat(_simulatorPosition);

            foreach (var ms in _motionSources)
            {
                ms.OnInitPluginSettings(this);
            }
        }
        
        private void InitOptionsUI()
        {
            var toggle = CreateToggle(_pauseLaunchMessages);
            toggle.label = "Pause Launch";
            
            var slider = CreateSlider(_simulatorPosition);
            slider.label = "Simulator";
            
            CreateScrollablePopup(_motionSourceChooser);

            CreateSpacer();
        }

        private void InitStorableActions()
        {
            JSONStorableAction startLaunchAction = new JSONStorableAction("startLaunch", () =>
            {
                _pauseLaunchMessages.SetVal(false);
            });
            RegisterAction(startLaunchAction);
            
            JSONStorableAction stopLaunchAction = new JSONStorableAction("stopLaunch", () =>
            {
                _pauseLaunchMessages.SetVal(true);
            });
            RegisterAction(stopLaunchAction);
            
            JSONStorableAction toggleLaunchAction = new JSONStorableAction("toggleLaunch", () =>
            {
                _pauseLaunchMessages.SetVal(!_pauseLaunchMessages.val);
            });
            RegisterAction(toggleLaunchAction);
        }

        private int GetMotionSourceIndex(string srcName)
        {
            return _motionSourceChoices.IndexOf(srcName);
        }
        
        private void UpdateMotionSource()
        {
            SetMotionSource();
            
            if (_currentMotionSource == null)
            {
                return;
            }
            
            byte pos = 0;
            byte speed = 0;
            if (_currentMotionSource.OnUpdate(ref pos, ref speed))
            {
                SendLaunchPosition(pos, speed);
            }
        }

        private void SetMotionSource()
        {
            if (_desiredMotionSourceIndex == _currentMotionSourceIndex)
            {
                return;
            }
            
            if (_currentMotionSource != null)
            {
                _currentMotionSource.OnDestroy(this);
                _currentMotionSource = null;
            }

            if (_desiredMotionSourceIndex >= 0)
            {
                _currentMotionSource = _motionSources[_desiredMotionSourceIndex];
                _currentMotionSource.OnInit(this);
            }

            _currentMotionSourceIndex = _desiredMotionSourceIndex;
        }
        
        private void OnDestroy()
        {
            StopNetwork();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void StopNetwork()
        {
            if (_network == null) return;
            SuperController.LogMessage("Shutting down VAM Launch network.");
            _network.Stop();
        }

        private void Update()
        {
            UpdateMotionSource();
            UpdateNetwork();
            UpdateSimulator();
        }

        private void UpdateSimulator()
        {
            var prevPos = _simulatorPosition.val;

            var newPos = Mathf.MoveTowards(prevPos, _simulatorTarget,
                LaunchUtils.PredictDistanceTraveled(_simulatorSpeed, Time.deltaTime));
            
            _simulatorPosition.SetVal(newPos);

            _currentMotionSource?.OnSimulatorUpdate(prevPos, newPos, Time.deltaTime);
        }

        private void SetSimulatorTarget(float pos, float speed)
        {
            _simulatorTarget = Mathf.Clamp(pos, 0.0f, LaunchUtils.LAUNCH_MAX_VAL);
            _simulatorSpeed = Mathf.Clamp(speed, 0.0f, LaunchUtils.LAUNCH_MAX_VAL);
        }
        
        // Not really used yet, but there just in case we want to do two-way communication between server
        private void UpdateNetwork()
        {
            if (_network == null)
            {
                return;
            }

            _networkPollTimer -= Time.deltaTime;
            if (!(_networkPollTimer <= 0.0f)) return;
            _networkPollTimer = NetworkListenInterval - Mathf.Min(-_networkPollTimer, NetworkListenInterval);
        }

        private void SendLaunchPosition(byte pos, byte speed)
        {
            SetSimulatorTarget(pos, speed);
            
            if (_network == null || _pauseLaunchMessages.val)
            {
                return;
            }

            float dist = Mathf.Abs(pos - _lastSentLaunchPos);
            var duration = LaunchUtils.PredictMoveDuration(dist, speed);
                
            //SuperController.LogMessage(string.Format("Sending: P:{0}, S:{1}, D:{2}", pos, speed, duration));
                
            _network.Send("LaunchData");
            _lastSentLaunchPos = pos;
        }
    }
}