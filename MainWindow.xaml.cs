using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Media;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Edu_board_game
{
    // 최상위 조건 <1> (델리게이트 선언)
    public delegate void GameLogHandler(string message);
    public delegate void ScoreChangedHandler(int score);
    public delegate void GameStateHandler(bool isRunning);

    public partial class MainWindow : Window
    {
        // 게임 제어 변수 설정
        private IBoardGame currentGame;
        private int currentScore = 0;

        // 배경색 브러시 정의(성능 및 재사용성 향상 목적)
        private readonly SolidColorBrush defaultBgBrush  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F7FA"));
        private readonly SolidColorBrush hintBgBrush = new SolidColorBrush(Colors.LightYellow);
        private readonly SolidColorBrush successBgBrush = new SolidColorBrush(Colors.LightGreen);
        private readonly SolidColorBrush failBgBrush = new SolidColorBrush(Colors.LightPink);

        public MainWindow()
        {
            InitializeComponent();
        }

        // 최상위 조건 <2> (게임 UI 시각화 제어 메서드)
        private void RefreshVisualScreen(string message)
        {
            if (currentGame == null) return;

            // 플레이스홀더 텍스트 숨김
            tbVisualPlaceholder.Visibility = Visibility.Collapsed;

            // 하노이의 탑 시각화 (도형 쌓기)
            if (currentGame is HanoiTowerGame hanoi)
            {
                HanoiCanvers.Visibility = Visibility.Visible;
                ItemWarpPanel.Visibility = Visibility.Collapsed;

                // 턴이 진행될때마다 캔버스를 지우고 원반을 다시 생성
                if (message.Contains("[원반 이동]") || message.Contains("시작"))
                {
                    HanoiCanvers.Children.Clear();
                    DrawHanoiTowers(hanoi.Moves);
                }
            }

            // 치킨차차 시작화 (꼬리깃털 늘리기)
            else if (currentGame is ChickenChaChaGame chacha)
            {
                HanoiCanvers.Visibility = Visibility.Collapsed;
                ItemWarpPanel.Visibility = Visibility.Visible;

                if (message.Contains("시작")) 
                {
                    ItemWarpPanel.Children.Clear();
                }

                if (message.Contains("[성공]"))
                {
                    // 성공할 때마다 닭과 깃털을 추가한다.
                    TextBlock chickenText = new TextBlock { Text = "🐔🪶", FontSize = 40, Margin = new Thickness(5) };
                    ItemWarpPanel.Children.Add(chickenText);
                }
            }

            // 우봉고 시각화 (보석 상자 채우기)
        }
        
        /// <1> 델리게이트 콜백 메서드 (WPF UI 스레드의 안정성 확보)

        // 1. 게임 로그 송수신
        private void UpdateGameLog(string message)
        {
            // WPF 크로스 스레드 처리
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateGameLog(message));
                return;
            }

            string safeMessage = message ?? "";

            lbLogs.Items.Add(safeMessage);
            lbLogs.ScrollIntoView(lbLogs.Items[lbLogs.Items.Count - 1]); // 스크롤 최하단 자동 이동

            // 학습자 편의 기능 지원: 키워드별 화면 색상 변환 및 효과음 적용
            if (safeMessage.Contains("[💡힌트]"))
            {
                SystemSounds.Asterisk?.Play();
                GameWindow.Background = hintBgBrush;
            }
            else if (safeMessage.Contains("[성공이에요!]") || safeMessage.Contains("[퍼즐을 완성했어요!]"))
            {
                SystemSounds.Asterisk?.Play();
                GameWindow.Background = successBgBrush;
            }
            else if (safeMessage.Contains("[실수했어요..]")) // 실패라는 완곡적인 표현보다는 학습자들의 연령대에 맞춘 문구를 출력한다.
            {
                SystemSounds.Asterisk?.Play();
                GameWindow.Background = failBgBrush;
            }
            else
            {
                GameWindow.Background = defaultBgBrush;
            }
        }

        // 2. 점수 송수신
        private void UpdateScore(int score)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateScore(score));
                return;
            }

            currentScore = score;

            int starCount = Math.Min(10, score / 30); // 별 모양 이모티콘으로 점수 표현
            string stars = starCount == 0 ? "시작해봐요!" : new string('⭐', starCount);

            lblScore.Text = $"내 칭찬 점수: {stars} ({currentScore}점)";
        }

        // 3. 게임 상태 송수신
        private void SetGameState(bool isRunning)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetGameState(isRunning));
                return;
            }

            cbGameSelector.IsEnabled = !isRunning; // 선택창과 시작 버튼은 게임 중 비활성화
            btnStart.IsEnabled = !isRunning;

            btnAction.IsEnabled = isRunning; // 플레이, 힌트, 정지 버튼은 게임 중 활성화
            btnHint.IsEnabled = isRunning;
            btnStop.IsEnabled = isRunning;
        }

        /// <2> 기본 UI 컨트롤 이벤트 핸들러 설정

        // 2-1. WPF의 ComboBoxItem 내용 안전하게 출력 
        private void cbGameSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbGameSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedGame = selectedItem.Content?.ToString() ?? "";

                if (string.IsNullOrEmpty(selectedGame))
                    return;

                lblGameTitle.Text = "🎮 " + selectedGame;
                lbLogs.Items.Clear();
                UpdateScore(0);

                switch (selectedGame)
                {
                    case "하노이의 탑": currentGame = new HanoiTowerGame(); break;
                    case "치킨차차": currentGame = new ChickenChaChaGame(); break;
                    case "우봉고": currentGame = new UbongoGame(); break;
                }

                UpdateGameLog($"{selectedGame}이(가) 선택되었습니다. 시작 버튼을 누르세요.");
            }
        }
        
        // 2-2. 버튼 컨트롤러 설정(버튼을 누를시 작동하는 행동들의 기본적인 정의)
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (currentGame == null)
                return;

            currentGame.OnLogReceived += UpdateGameLog;
            currentGame.OnScoreChanged += UpdateScore;
            currentGame.OnStateChanged += SetGameState;

            currentGame.Start();
        }

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            currentGame?.PlayTurn();
        }

        private void btnHint_Click(object sender, RoutedEventArgs e)
        {
            currentGame?.ProvideHint();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (currentGame != null)
            {
                currentGame.Stop();

                currentGame.OnLogReceived -= UpdateGameLog;
                currentGame.OnScoreChanged -= UpdateScore;
                currentGame.OnStateChanged -= SetGameState;
            }
        }
    }

    /// <3> 보드게임 공통 인터페이스 설정
    public interface IBoardGame
    {
        // 게임의 작동 규칙 컨트롤러
        string Name { get; }
        event GameLogHandler OnLogReceived;
        event ScoreChangedHandler OnScoreChanged;
        event GameStateHandler OnStateChanged;

        void Start(); // 시작 인터페이스
        void PlayTurn(); // 행동 인터페이스
        void ProvideHint(); // 게임 힌트 인터페이스
        void Stop(); // 게임 종료 인터페이스
    }

    /// <4> 보드게임별 작동 로직

    // 4-1. 하노이의 탑
    public class HanoiTowerGame : IBoardGame
    {
        public string Name => "하노이의 탑";
        public event GameLogHandler OnLogReceived;
        public event ScoreChangedHandler OnScoreChanged;
        public event GameStateHandler OnStateChanged;

        private int moves = 0;

        public void Start()
        {
            moves = 0;
            OnLogReceived?.Invoke("★ 하노이의 탑 게임을 시작합니다.");
            OnStateChanged?.Invoke(true);
        }

        public void PlayTurn()
        {
            moves++;
            OnLogReceived?.Invoke($"[원반 이동] {moves}번째 원반을 알맞게 옮겼습니다.");
            OnScoreChanged?.Invoke(moves * 10);
        }

        public void ProvideHint()
        {
            OnLogReceived?.Invoke("[💡힌트] 기억하세요! 큰 원반은 절대로 작은 원반 위로 올라갈 수 없어요! 가장 위에 있는 작은 원반부터 차근차근 옮겨봐요.");
        }

        public void Stop()
        {
            OnLogReceived?.Invoke($"■ 게임 종료. 내가 해낸 총 이동 횟수는: {moves}회 이동시켰어요.");
            OnStateChanged?.Invoke(false);
        }
    }

    // 4-2. 치킨차차
    public class ChickenChaChaGame : IBoardGame
    {
        public string Name => "치킨차차";
        public event GameLogHandler OnLogReceived;
        public event ScoreChangedHandler OnScoreChanged;
        public event GameStateHandler OnStateChanged;

        private int feathers = 0;
        private Random rand = new Random();
        private string[] tileImages = { "귀여운 오리", "싱싱한 채소", "예쁜 꽃", "매끄러운 조약돌" };

        public void Start()
        {
            feathers = 0;
            OnLogReceived?.Invoke("★ 치킨차차 메모리 게임을 시작합니다.");
            OnStateChanged?.Invoke(true);
        }

        public void PlayTurn()
        {
            bool isMatch = rand.Next(0, 2) == 1;
            if (isMatch)
            {
                feathers++;
                OnLogReceived?.Invoke($"[성공] 그림이 일치합니다! 꼬리깃털을 획득했습니다. (현재 {feathers}개)");
                OnScoreChanged?.Invoke(feathers * 30);
            }
            else
            {
                OnLogReceived?.Invoke("[실패] 이런 그림이 달라요. 다음 마당 타일을 잘 기억해두세요!");
            }
        }

        public void ProvideHint()
        {
            string hintTile = tileImages[rand.Next(tileImages.Length)];
            OnLogReceived?.Invoke($"[💡힌트] 쉿! 이건 비밀인데 앞쪽 어딘가에 {{hintTile}} 그림 타일이 숨겨져 있는 것 같아요!");
        }

        public void Stop()
        {
            OnLogReceived?.Invoke($"■ 게임 종료. 내가 획득한 꼬리깃털의 갯수는?: {feathers}개");
            OnStateChanged?.Invoke(false);
        }
    }

    // 4-3. 우봉고
    public class UbongoGame : IBoardGame
    {
        public string Name => "우봉고";
        public event GameLogHandler OnLogReceived;
        public event ScoreChangedHandler OnScoreChanged;
        public event GameStateHandler OnStateChanged;

        private int solvedPuzzles = 0;
        private Random rand = new Random();

        public void Start()
        {
            solvedPuzzles = 0;
            OnLogReceived?.Invoke("★ 우봉고 퍼즐 게임을 시작합니다!");
            OnStateChanged?.Invoke(true);
        }

        public void PlayTurn()
        {
            solvedPuzzles++;
            OnLogReceived?.Invoke($"[퍼즐 완성을 완성했어요!] \"우봉고!\" 외치며 퍼즐판을 모두 채워 반짝이는 보석을 얻었습니다.");
            OnScoreChanged?.Invoke(solvedPuzzles * 50);
        }

        public void ProvideHint()
        {
            OnLogReceived?.Invoke("[💡힌트] 퍼즐 조각이 안 맞을 때는 마우스로 조각을 돌리거나 뒤집어 보세요! 그리고 가장 커다란 조각부터 먼저 자리를 잡아주면 훨씬 쉬워요!");

        }

        public void Stop()
        {
            OnLogReceived?.Invoke($"■ 게임 종료. 내가 해결한 총 퍼즐의 갯수는: {solvedPuzzles}개");
            OnStateChanged?.Invoke(false);
        }
    }
}
