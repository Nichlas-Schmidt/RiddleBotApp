
namespace Riddle_Discord_Bot
{
    public interface IRiddleGame
    {
        public string? CorrectAnswer { get; set; }
        public string? RiddleText { get; set; }
        public void StartGame();
        public void StopGame();
        public bool Guess(string input);
        public void NextRiddle();
    }
}
