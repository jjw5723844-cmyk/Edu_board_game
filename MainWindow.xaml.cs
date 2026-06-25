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
    public partial class MainWindow : Window
    {
        // 변수 및 필드 정의
        private IBoardGame currentGame;
        private int currentScore = 0;
        private Random visualRand = new Random();

        private Brush hintBgBrush = Brushes.LightYellow;
        private Brush successBgBrush = Brushes.LightGreen;
        private Brush failBgBrush = Brushes.MistyRose;
        private Brush defaultBgBrush = Brushes.White;

        public MainWindow()
        {
            InitializeComponent();
        }

        // 실시간 화면 갱신 총괄 메서드
        private void RefreshVisualScreen(string message)
        {
            if (currentGame is HanoiTowerGame hanoi)
            {
                HanoiCanvas.Visibility = Visibility.Visible;
                ItemWrapPanel.Visibility = Visibility.Collapsed;
                tbVisualPlaceholder.Visibility = Visibility.Collapsed;

                DrawHanoiTowers(hanoi.Moves);
            }
            // 우봉고 시각화 (4x4 퍼즐 격자 보드판과 채워지는 블록 구현)
            else if (currentGame is UbongoGame ubongo)
            {
                HanoiCanvas.Visibility = Visibility.Collapsed;
                ItemWrapPanel.Visibility = Visibility.Visible;
                tbVisualPlaceholder.Visibility = Visibility.Collapsed;

                if (message.Contains("시작") || ItemWrapPanel.Children.Count != 16)
                {
                    ItemWrapPanel.Children.Clear();
                    // 우봉고 퍼즐 보드판 (4x4 총 16칸 Grid 모사)
                    for (int i = 0; i < 16; i++)
                    {
                        Border puzzleCell = new Border
                        {
                            Width = 60,
                            Height = 60,
                            Margin = new Thickness(4),
                            Background = Brushes.LightGray,
                            BorderBrush = Brushes.DarkGray,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(5)
                        };

                        ItemWrapPanel.Children.Add(puzzleCell);
                    }
                }

                // 퍼즐 완성 시 빈 퍼즐 그리드 칸들이 알록달록한 퍼즐 조각 색상으로 채워짐
                if (message.Contains("[퍼즐을 완성했어요!]"))
                {
                    Brush[] puzzleColors = { Brushes.MediumOrchid, Brushes.DodgerBlue, Brushes.YellowGreen, Brushes.IndianRed, Brushes.Gold };
                    string[] gemEmojis = { "💎", "🔮", "👑", "⭐" };

                    // 턴마다 무작위 칸 3~4개를 색상 블록과 보석으로 채워 퍼즐 판 맞춤을 시각화
                    for (int i = 0; i < ItemWrapPanel.Children.Count; i++)
                    {
                        if (ItemWrapPanel.Children[i] is Border cell && visualRand.Next(0, 3) == 1)
                        {
                            cell.Background = puzzleColors[visualRand.Next(puzzleColors.Length)];
                            cell.BorderBrush = Brushes.White;
                            cell.Child = new TextBlock
                            {
                                Text = gemEmojis[visualRand.Next(gemEmojis.Length)],
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 18
                            };
                        }
                    }
                }
            }
            // 치킨차차 시각화 (12개의 예쁜 원형 카드 레이아웃 구현)
            else if (currentGame is ChickenChaChaGame chicken)
            {
                HanoiCanvas.Visibility = Visibility.Collapsed;
                tbVisualPlaceholder.Visibility = Visibility.Collapsed;
                ItemWrapPanel.Visibility = Visibility.Visible;

                // 12개의 동그란 "can" 카드판 생성
                if (message.Contains("시작") || ItemWrapPanel.Children.Count != 12)
                {
                    ItemWrapPanel.Children.Clear();
                    for (int i = 1; i <= 12; i++)
                    {
                        Border circleCard = new Border
                        {
                            Width = 75,
                            Height = 75,
                            Margin = new Thickness(10, 8, 10, 8),
                            Background = new SolidColorBrush(Color.FromRgb(254, 243, 226)), // 연한 주황빛 톤
                            BorderBrush = new SolidColorBrush(Color.FromRgb(249, 168, 37)),  // 따뜻한 오렌지 테두리
                            BorderThickness = new Thickness(2),
                            CornerRadius = new CornerRadius(37.5), // 원형 구현
                            Child = new TextBlock
                            {
                                Text = $"can {i}",
                                Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0)),
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 13
                            }
                        };
                        ItemWrapPanel.Children.Add(circleCard);
                    }
                }

                // 성공 시 카드 한 장이 뒤집히거나 깃털을 획득하는 시각 효과 추가 연출
                if (message.Contains("[성공이에요!]"))
                {
                    int luckyIdx = visualRand.Next(0, 12);
                    if (ItemWrapPanel.Children[luckyIdx] is Border card)
                    {
                        card.Background = Brushes.LightGreen;
                        card.BorderBrush = Brushes.Green;
                        if (card.Child is TextBlock tb)
                        {
                            tb.Text = "🐓 꼬리!";
                            tb.Foreground = Brushes.DarkGreen;
                        }
                    }
                }
            }
        }

        // 하노이의 탑 시각화
        private void DrawHanoiTowers(int currentMoves)
        {
            HanoiCanvas.Children.Clear();
            double canvasWidth = VisualScreen.ActualWidth;
            if (canvasWidth == 0) canvasWidth = 500;

            double groundY = 240; // 바닥 기둥 바의 Y축 위치

            // 1. 기둥 베이스 바닥 그리기
            Rectangle ground = new Rectangle { Width = canvasWidth - 40, Height = 12, Fill = Brushes.SaddleBrown };
            Canvas.SetLeft(ground, 20);
            Canvas.SetTop(ground, groundY);
            HanoiCanvas.Children.Add(ground);

            // 2. 기둥 3개 위치 및 하단 레이블
            double[] pegX = { canvasWidth * 0.25, canvasWidth * 0.5, canvasWidth * 0.75 };
            string[] pegNames = { "기둥 A", "기둥 B", "기둥 C" };

            for (int i = 0; i < 3; i++)
            {
                Rectangle pillar = new Rectangle { Width = 10, Height = 140, Fill = Brushes.DarkGray, RadiusX = 2, RadiusY = 2 };
                Canvas.SetLeft(pillar, pegX[i] - 5);
                Canvas.SetTop(pillar, groundY - 140);
                HanoiCanvas.Children.Add(pillar);

                TextBlock pegLabel = new TextBlock
                {
                    Text = pegNames[i],
                    Foreground = Brushes.DimGray,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Width = 60,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(pegLabel, pegX[i] - 30);
                Canvas.SetTop(pegLabel, groundY + 18);
                HanoiCanvas.Children.Add(pegLabel);
            }

            // 3. 하노이의 탑 규칙 적용(3개 원반의 이동 경로를 완벽하게 추적하는 상태 배열 구현)

            // 하노이의 탑 3층이 구성될 경우 총 7번의 이동한다. (% 8을 통한 무한 반복 가능)
            int step = currentMoves % 8;


            /* 원반의 인덱스 정의
             1. index 0: 제일 큰 원반, index 1: 중간 원반, index 2: 제일 작은 원반
             2. 각 원반이 현재 어떤 기둥(0:A, 1:B, 2:C)에 가 있어야 하는지 정의
            */
            int[] diskPegs = new int[3];

            // 원반이 쌓이는 규칙 정의
            switch (step)
            {
                case 0: diskPegs = new int[] { 0, 0, 0 }; break; // 첫 단계: 모두 A에 쌓임 (3층)
                case 1: diskPegs = new int[] { 0, 0, 2 }; break; // 작은 원반 -> C
                case 2: diskPegs = new int[] { 0, 1, 2 }; break; // 중간 원반 -> B
                case 3: diskPegs = new int[] { 0, 1, 1 }; break; // 작은 원반 -> B (B에 2층 쌓임)
                case 4: diskPegs = new int[] { 2, 1, 1 }; break; // 큰 원반 -> C
                case 5: diskPegs = new int[] { 2, 1, 0 }; break; // 작은 원반 -> A
                case 6: diskPegs = new int[] { 2, 2, 0 }; break; // 중간 원반 -> C (C에 2층 쌓임)
                case 7: diskPegs = new int[] { 2, 2, 2 }; break; // 최종 단계: 모두 C에 정렬 (3층)
            }

            // 원반 설정 (큰 것부터 차례대로 밑바닥에 깔리도록 순서 고정)
            int diskCount = 3;
            double[] diskWidths = { 130, 95, 60 }; // 크기 밸런스 조정
            Brush[] diskColors = { Brushes.Tomato, Brushes.Gold, Brushes.DodgerBlue };

            // 각 기둥별로 현재 몇 층까지 원반이 쌓였는지 확인하는 카운터
            int[] pegDiskCounts = { 0, 0, 0 };

            for (int i = 0; i < diskCount; i++)
            {
                double diskWidth = diskWidths[i];
                double diskHeight = 22;

                Rectangle disk = new Rectangle { Width = diskWidth, Height = diskHeight, Fill = diskColors[i], RadiusX = 5, RadiusY = 5 };

                // 하노이 탑 작동 원리 규칙에서 가져온 목적지 기둥 구하기
                int targetPeg = diskPegs[i];

                double leftPos = pegX[targetPeg] - (diskWidth / 2);

                // 해당 기둥에 이미 쌓인 개수만큼 위로 쌓아 원반별 중첩되는 현상을 방지
                double topPos = groundY - diskHeight - (pegDiskCounts[targetPeg] * diskHeight);

                // 해당 기둥에 쌓이는 카운트 증가
                pegDiskCounts[targetPeg]++;

                Canvas.SetLeft(disk, leftPos);
                Canvas.SetTop(disk, topPos);
                HanoiCanvas.Children.Add(disk);
            }
        }

        /// <1> 델리게이트 콜백 메서드 (WPF UI 스레드의 안정성 확보)

        // 1. 게임 로그 송수신
        private void UpdateGameLog(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateGameLog(message));
                return;
            }

            string safeMessage = message ?? "";

            lbLogs.Items.Add(safeMessage);
            lbLogs.ScrollIntoView(lbLogs.Items[lbLogs.Items.Count - 1]);

            // 실시간 화면 갱신
            RefreshVisualScreen(safeMessage);

            // 학습자 편의 기능 지원: 키워드별 화면 색상 변환 및 효과음 적용
            if (safeMessage.Contains("[💡힌트]"))
            {
                SystemSounds.Asterisk?.Play();
                this.Background = hintBgBrush;
            }
            else if (safeMessage.Contains("[성공이에요!]") || safeMessage.Contains("[퍼즐을 완성했어요!]"))
            {
                SystemSounds.Asterisk?.Play();
                this.Background = successBgBrush;
            }
            else if (safeMessage.Contains("[실수했어요..]")) // 실패라는 완곡적인 표현보다는 학습자들의 연령대에 맞춘 문구를 출력한다.
            {
                SystemSounds.Asterisk?.Play();
                this.Background = failBgBrush;
            }
            else
            {
                this.Background = defaultBgBrush;
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

            lblScore.Text = $"내 칭찬 점수는: {stars} ({currentScore}점)";
        }

        // 3. 게임 상태 송수신
        private void SetGameState(bool isRunning)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetGameState(isRunning));
                return;
            }

            cbGameSelector.IsEnabled = !isRunning;
            btnStart.IsEnabled = !isRunning;

            btnAction.IsEnabled = isRunning;
            btnHint.IsEnabled = isRunning;
            btnStop.IsEnabled = isRunning;

            if (!isRunning)
            {
                tbVisualPlaceholder.Visibility = Visibility.Visible;
                tbVisualPlaceholder.Text = "게임이 종료되었습니다! 다른 게임을 골라보세요. 🔄";
                HanoiCanvas.Visibility = Visibility.Collapsed;
                ItemWrapPanel.Visibility = Visibility.Collapsed;
            }
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

    // 통신을 연결하기 위한 델리게이트 정의
    public delegate void GameLogHandler(string message);
    public delegate void ScoreChangedHandler(int score);
    public delegate void GameStateHandler(bool isRunning);

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

        // 외부(UI)에서 get 속성을 인삭하여 안전하게 검토
        public int Moves => moves;

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
            OnLogReceived?.Invoke("[💡힌트] 기억하세요! 큰 원반은 절대로 작은 원반 위로 올라갈 수 없어요! \n가장 위에 있는 작은 원반부터 차근차근 옮겨봐요.");
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
        public int Feathers => feathers;
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
                OnLogReceived?.Invoke($"[성공이에요!] 그림이 일치합니다! 꼬리깃털을 획득했습니다. (현재 {feathers}개)");
                OnScoreChanged?.Invoke(feathers * 30);
            }
            else
            {
                OnLogReceived?.Invoke("[실수했어요..] 이런 그림이 달라요. 다음 마당 타일을 잘 기억해두세요!");
            }
        }

        public void ProvideHint()
        {
            string hintTile = tileImages[rand.Next(tileImages.Length)];
            OnLogReceived?.Invoke($"[💡힌트] 쉿! 이건 비밀인데 앞쪽 어딘가에 {hintTile} 그림 타일이 숨겨져 있는 것 같아요!");
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

        public void Start()
        {
            solvedPuzzles = 0;
            OnLogReceived?.Invoke("★ 우봉고 퍼즐 게임을 시작합니다!");
            OnStateChanged?.Invoke(true);
        }

        public void PlayTurn()
        {
            solvedPuzzles++;
            OnLogReceived?.Invoke($"[퍼즐을 완성했어요!] \"우봉고!\" 외치며 퍼즐판을 모두 채워 반짝이는 보석을 얻었습니다.");
            OnScoreChanged?.Invoke(solvedPuzzles * 50);
        }

        public void ProvideHint()
        {
            OnLogReceived?.Invoke("[💡힌트] 퍼즐 조각이 안 맞을 때는 마우스로 조각을 돌리거나 뒤집어 보세요! \n그리고 가장 커다란 조각부터 먼저 자리를 잡아주면 훨씬 쉬워요!");
        }

        public void Stop()
        {
            OnLogReceived?.Invoke($"■ 게임 종료. 내가 해결한 총 퍼즐의 갯수는: {solvedPuzzles}개");
            OnStateChanged?.Invoke(false);
        }
    }
}