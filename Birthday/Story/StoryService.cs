using System;
using System.Collections.Generic;
using System.Linq;

namespace Birthday.Story
{
    public record StoryChoice(string Text, string? NextId);

    public class StoryStep
    {
        public string Id { get; init; } = string.Empty;
        public string? Title { get; init; }
        public string? BackgroundImage { get; init; }
        public string? CharacterImage { get; init; }
        public string? CharacterName { get; init; }
        public string? Dialogue { get; init; }
        public List<StoryChoice> Choices { get; init; } = new();
        public string? NextId { get; init; }

        public bool IsFinal => string.IsNullOrWhiteSpace(NextId) && Choices.Count == 0;
    }

    public class StoryService
    {
        private readonly Dictionary<string, StoryStep> _steps;
        private readonly string _entryId;
        private StoryStep? _currentStep;

        public event Action<StoryStep>? StepChanged;
        public event Action? StoryCompleted;

        private StoryService(IEnumerable<StoryStep> steps, string entryId)
        {
            _steps = steps.ToDictionary(step => step.Id);
            _entryId = entryId;
        }

        public static StoryService CreateDemoStory()
        {
            var steps = new List<StoryStep>
            {
                new StoryStep
                {
                    Id = "intro",
                    Title = "序章 · 北境風雪",
                    BackgroundImage = "Assets/Image/Scenario/主城一.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "長城上的風雪終於停歇，豹豹望著遠方微亮的天際，心知今夜的平靜只是暴風雨前的寧靜。",
                    NextId = "briefing"
                },
                new StoryStep
                {
                    Id = "briefing",
                    Title = "序章 · 盟友集結",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "貓貓帶來了剛從前線回傳的情報：敵軍大將集結於官門一帶，今晚便會發兵。",
                    NextId = "council"
                },
                new StoryStep
                {
                    Id = "council",
                    Title = "序章 · 軍議廳",
                    BackgroundImage = "Assets/Image/Scenario/官門.jpg",
                    CharacterImage = "Assets/Image/Figure/女將軍_全身.jpg",
                    CharacterName = "女將軍",
                    Dialogue = "女將軍拍案而起：『我們必須先發制人，守株待兔只會讓士氣潰散。』",
                    NextId = "strategy"
                },
                new StoryStep
                {
                    Id = "strategy",
                    Title = "序章 · 選擇出擊",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "豹豹深吸一口氣，向眾人提出兩種可能的突擊方案。",
                    Choices =
                    {
                        new StoryChoice("趁夜色掩護，奇襲敵營", "stealth"),
                        new StoryChoice("號角齊鳴，正面突擊", "charge")
                    }
                },
                new StoryStep
                {
                    Id = "stealth",
                    Title = "序章 · 月下奇襲",
                    BackgroundImage = "Assets/Image/Scenario/寺廟.jpg",
                    CharacterImage = "Assets/Image/Figure/貓貓_全身.jpg",
                    CharacterName = "貓貓",
                    Dialogue = "夜色沉沉，奇襲小隊在貓貓的引導下潛入敵營，取下了守軍的號角。",
                    NextId = "battle"
                },
                new StoryStep
                {
                    Id = "charge",
                    Title = "序章 · 鼓聲震天",
                    BackgroundImage = "Assets/Image/Scenario/敵城.jpg",
                    CharacterImage = "Assets/Image/Figure/女將軍_全身.jpg",
                    CharacterName = "女將軍",
                    Dialogue = "戰鼓雷鳴，女將軍策馬當先衝破敵陣，士氣如同烈焰般席捲。",
                    NextId = "battle"
                },
                new StoryStep
                {
                    Id = "battle",
                    Title = "序章 · 終局對決",
                    BackgroundImage = "Assets/Image/Scenario/主城二.jpg",
                    CharacterImage = "Assets/Image/Figure/Boss.jpg",
                    CharacterName = "敵軍大將",
                    Dialogue = "不論是奇襲成功或是正面制勝，最後的戰場都匯聚在主城門前，敵軍大將終於現身。",
                    NextId = "epilogue"
                },
                new StoryStep
                {
                    Id = "epilogue",
                    Title = "序章 · 迎向黎明",
                    BackgroundImage = "Assets/Image/Scenario/庭院.jpg",
                    CharacterImage = "Assets/Image/Figure/豹豹_全身.jpg",
                    CharacterName = "豹豹",
                    Dialogue = "黎明的曙光灑落城牆，豹豹收起長劍，心底默默發誓：真正的旅程，才正要開始。"
                }
            };

            return new StoryService(steps, "intro");
        }

        public void Start() => Start(_entryId);

        public void Start(string stepId)
        {
            if (!_steps.TryGetValue(stepId, out var step))
            {
                throw new ArgumentException($"找不到劇情節點 {stepId}", nameof(stepId));
            }

            _currentStep = step;
            StepChanged?.Invoke(step);
        }

        public void Continue()
        {
            if (_currentStep == null)
            {
                StoryCompleted?.Invoke();
                return;
            }

            if (_currentStep.Choices.Count > 0)
            {
                // 等待玩家做出選擇
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentStep.NextId))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            if (!_steps.TryGetValue(_currentStep.NextId, out var nextStep))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            _currentStep = nextStep;
            StepChanged?.Invoke(nextStep);
        }

        public void Choose(string? nextId)
        {
            if (string.IsNullOrWhiteSpace(nextId))
            {
                Continue();
                return;
            }

            if (!_steps.TryGetValue(nextId, out var nextStep))
            {
                _currentStep = null;
                StoryCompleted?.Invoke();
                return;
            }

            _currentStep = nextStep;
            StepChanged?.Invoke(nextStep);
        }
    }
}