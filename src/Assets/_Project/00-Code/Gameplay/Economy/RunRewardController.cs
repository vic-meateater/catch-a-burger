using System;
using BurgerCatch.Core.Economy;
using BurgerCatch.Core.Leaderboard;
using BurgerCatch.Core.Saves;
using BurgerCatch.Data;
using BurgerCatch.Events;
using BurgerCatch.Gameplay.Scoring;
using UnityEngine;
using Zenject;

namespace BurgerCatch.Gameplay.Economy
{
  /// <summary>
  /// Итоги забега на game over: начислить валюту за счёт, обновить рекорд и
  /// отправить его в лидерборд.
  /// </summary>
  public sealed class RunRewardController : IInitializable, IDisposable
  {
    private readonly SignalBus _signalBus;
    private readonly ScoringSystem _scoring;
    private readonly ICurrencyService _currency;
    private readonly ILeaderboardService _leaderboard;
    private readonly ISaveService _saveService;
    private readonly GameplayConfig _config;

    public RunRewardController(
      SignalBus signalBus,
      ScoringSystem scoring,
      ICurrencyService currency,
      ILeaderboardService leaderboard,
      ISaveService saveService,
      GameplayConfig config)
    {
      _signalBus = signalBus;
      _scoring = scoring;
      _currency = currency;
      _leaderboard = leaderboard;
      _saveService = saveService;
      _config = config;
    }

    public void Initialize()
    {
      _signalBus.Subscribe<GameOverTriggeredSignal>(OnGameOver);
    }

    public void Dispose()
    {
      _signalBus.Unsubscribe<GameOverTriggeredSignal>(OnGameOver);
    }

    private void OnGameOver(GameOverTriggeredSignal _)
    {
      int score = _scoring.RunScore;

      // Заработок за забег по коэффициенту из конфига.
      int reward = Mathf.RoundToInt(score * _config.CurrencyPerScore);
      _currency.Add(reward);

      // Рекорд: обновляем только если побит.
      if (score > _saveService.Data.BestScore)
      {
        _saveService.Data.BestScore = score;
        _saveService.Save();
        _signalBus.Fire(new BestScoreChangedSignal(score));
        _leaderboard.SubmitScore(score);
      }
    }
  }
}
