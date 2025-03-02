using System;
using System.Collections.Generic;
using LoVaMPlugin.MotionSources;
using LoVaMPlugin.Network;
using LoVaMPlugin.Toys;
using UnityEngine;

namespace LoVaMPlugin
{
    public class LoVaM : MVRScript
    {
        private static LoVaM _instance;
        
        private const string ServerIP = "192.168.178.73";
        private const int ServerListenPort = 20010;
        
        private INetwork _network;

        private byte _lastSentLaunchPos;

        private JSONStorableStringChooser _motionSourceChooser;
        private JSONStorableBool _pauseLaunchMessages;
        private JSONStorableFloat _simulatorPosition;
        
        private float _simulatorTarget;
        private float _simulatorSpeed;

        private IMotionSource _currentMotionSource;
        private int _currentMotionSourceIndex = -1;
        private int _desiredMotionSourceIndex;

        private const double SendPeriod = 0.25;
        private double _timeLastSend = Time.time;
        
        private IToy _toy;
        
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
            InitToy();
        }

        private void InitNetwork()
        {
            StopNetwork();

            _network = new LoVaMNetwork();
            _network.Init(ServerIP, ServerListenPort);
        }

        private void InitToy()
        {
            _toy = new SolaceTwo();
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

            var reconnectButton = CreateButton("Reconnect");
            reconnectButton.button.onClick.AddListener(() =>
            {
                _network.Reconnect();
            });
            
            var slider = CreateSlider(_simulatorPosition);
            slider.label = "Simulator";
            
            CreateScrollablePopup(_motionSourceChooser);

            CreateSpacer();
        }

        private void ReconnectNetwork()
        {
            
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
            
            JSONStorableAction reconnectNetworkAction = new JSONStorableAction("reconnectNetwork", () =>
            {
                _network.Reconnect();
            });
            RegisterAction(reconnectNetworkAction);
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
            _network?.Disconnect();
        }

        private void Update()
        {
            UpdateMotionSource();
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
        
        private void SendLaunchPosition(byte pos, byte speed)
        {
            SetSimulatorTarget(pos, speed);
            
            if (_network == null || _pauseLaunchMessages.val)
            {
                return;
            }

            var now = Time.time;
            float dist = Mathf.Abs(pos - _lastSentLaunchPos);
            var duration = LaunchUtils.PredictMoveDuration(dist, speed);

            //SuperController.LogMessage($"Time: LS:{_timeLastSend}, N:{now}");
            if (_timeLastSend < now)
            {
                var sendPos = Math.Abs(pos - duration * speed);
                SuperController.LogMessage($"Sending: P:{pos}, S:{speed}, Di:{dist}, Du:{duration}, Sp:{sendPos}");
                try
                {
                    _network.Send(_toy.GetCommand((byte)sendPos));
                }
                catch (Exception e)
                {
                    _pauseLaunchMessages.SetVal(true);
                    _network?.Disconnect();
                }
                
                _timeLastSend = now + SendPeriod;
            }

            _lastSentLaunchPos = pos;
        }
    }
}