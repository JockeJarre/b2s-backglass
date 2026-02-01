using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Rendering
{
    public class AnimationEngine : IDisposable
    {
        private Models.BackglassData? _backglassData;
        private Forms.BackglassForm? _form;
        private Dictionary<string, AnimationPlayer> _players = new Dictionary<string, AnimationPlayer>();

        public AnimationEngine(Models.BackglassData backglassData, Forms.BackglassForm form)
        {
            _backglassData = backglassData;
            _form = form;
        }

        public void StartAnimation(string animationName, bool reverse = false)
        {
            if (_backglassData == null || _form == null)
                return;

            var animation = _backglassData.Animations.FirstOrDefault(a => 
                a.Name.Equals(animationName, StringComparison.OrdinalIgnoreCase));

            if (animation == null)
            {
                System.Diagnostics.Debug.WriteLine($"Animation not found: {animationName}");
                return;
            }

            if (_players.ContainsKey(animationName))
            {
                _players[animationName].Stop();
                _players.Remove(animationName);
            }

            var player = new AnimationPlayer(animation, _backglassData, _form, reverse);
            _players[animationName] = player;
            player.Start();

            System.Diagnostics.Debug.WriteLine($"Started animation: {animationName}");
        }

        public void StopAnimation(string animationName)
        {
            if (_players.ContainsKey(animationName))
            {
                _players[animationName].Stop();
                _players.Remove(animationName);
                System.Diagnostics.Debug.WriteLine($"Stopped animation: {animationName}");
            }
        }

        public void StopAllAnimations()
        {
            foreach (var player in _players.Values)
            {
                player.Stop();
            }
            _players.Clear();
        }

        public void Dispose()
        {
            StopAllAnimations();
        }
    }

    internal class AnimationPlayer
    {
        private Models.Animation _animation;
        private Models.BackglassData _backglassData;
        private Forms.BackglassForm _form;
        private Timer _timer;
        private int _currentStep;
        private bool _reverse;

        public AnimationPlayer(Models.Animation animation, Models.BackglassData backglassData, 
            Forms.BackglassForm form, bool reverse)
        {
            _animation = animation;
            _backglassData = backglassData;
            _form = form;
            _reverse = reverse;
            _currentStep = reverse ? animation.Steps.Count - 1 : 0;

            _timer = new Timer();
            _timer.Interval = animation.Interval > 0 ? animation.Interval : 100;
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _animation.IsPlaying = true;
            ExecuteCurrentStep();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _animation.IsPlaying = false;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_reverse)
            {
                _currentStep--;
                if (_currentStep < 0)
                {
                    _currentStep = _animation.Steps.Count - 1;
                }
            }
            else
            {
                _currentStep++;
                if (_currentStep >= _animation.Steps.Count)
                {
                    _currentStep = 0;
                }
            }

            _animation.CurrentStep = _currentStep;
            ExecuteCurrentStep();
        }

        private void ExecuteCurrentStep()
        {
            if (_currentStep < 0 || _currentStep >= _animation.Steps.Count)
                return;

            var step = _animation.Steps[_currentStep];

            foreach (var bulbName in step.Bulbs)
            {
                if (string.IsNullOrEmpty(bulbName))
                    continue;

                var illumination = _backglassData.Illuminations.FirstOrDefault(i => 
                    i.Name.Equals(bulbName, StringComparison.OrdinalIgnoreCase));

                if (illumination != null)
                {
                    bool newState = step.Visible;
                    
                    if (illumination.IsOn != newState)
                    {
                        illumination.IsOn = newState;
                        _form.Invalidate();
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Animation '{_animation.Name}' step {_currentStep}/{_animation.Steps.Count}");
        }
    }
}
