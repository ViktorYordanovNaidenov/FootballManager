using System;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class MainMenuForm : Form
    {
        private string userRole;

        public MainMenuForm(string role)
        {
            InitializeComponent();
            this.userRole = role;
            SetupMenuUI();
            Theme.Apply(this);
        }

        private void SetupMenuUI()
        {
            this.Text = $"Football Manager - Главно меню ({userRole})";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;


            // Заглавие
            Label lblTitle = new Label()
            {
                Text = "Управление на Първенство",
                Font = new Font("Arial", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(150, 20)

            };
            this.Controls.Add(lblTitle);

            // Бутони за модулите (подредени един под друг)
            CreateMenuButton("Клубове и Отбори", 70, BtnClubs_Click);
            CreateMenuButton("Играчи", 130, BtnPlayers_Click);
            CreateMenuButton("Трансфери", 190, BtnTransfers_Click);
            CreateMenuButton("Първенства (Лиги)", 250, BtnLeagues_Click);
            CreateMenuButton("Мачове и Резултати", 310, BtnMatches_Click);
            CreateMenuButton("Класиране и Статистика", 370, BtnStats_Click);

        }

        // Помощен метод за създаване на еднакви бутони
        // Подобрен метод за красиви бутони в менюто
        private void CreateMenuButton(string text, int y, EventHandler onClick)
        {
            Button btn = new Button();

            // Добавяме Емоджита според текста за красота
            if (text.Contains("Клубове")) text = "🛡️ " + text;
            if (text.Contains("Играчи")) text = "⚽ " + text;
            if (text.Contains("Трансфери")) text = "💸 " + text;
            if (text.Contains("Първенства")) text = "🏆 " + text;
            if (text.Contains("Мачове")) text = "📅 " + text;
            if (text.Contains("Класиране")) text = "📊 " + text;

            btn.Text = text;
            btn.Location = new Point(100, y); // Центрираме ги малко по-добре
            btn.Size = new Size(400, 50);     // По-големи бутони
            btn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btn.Click += onClick;

            // Темата ще ги оцвети в синьо автоматично, но ние ще ги направим малко по-тъмни
            btn.BackColor = Color.FromArgb(41, 128, 185);
            btn.ForeColor = Color.White;

            this.Controls.Add(btn);
        }

        // --- ТУК ЩЕ СЛАГАМЕ ЛОГИКАТА ЗА ОТВАРЯНЕ НА ФОРМИТЕ ПО-КЪСНО ---

        private void BtnClubs_Click(object sender, EventArgs e)
        {
            // Отваряме формата като "Dialog" (не можеш да цъкаш отзад, докато не я затвориш)
            ClubsForm form = new ClubsForm();
            form.ShowDialog();
        }

        private void BtnPlayers_Click(object sender, EventArgs e)
        {
            PlayersForm form = new PlayersForm();
            form.ShowDialog();
        }

        private void BtnTransfers_Click(object sender, EventArgs e)
        {
            TransfersForm form = new TransfersForm();
            form.ShowDialog();
        }

        private void BtnLeagues_Click(object sender, EventArgs e)
        {
            LeaguesForm form = new LeaguesForm();
            form.ShowDialog();
        }

        private void BtnMatches_Click(object sender, EventArgs e)
        {
            MatchesForm form = new MatchesForm();
            form.ShowDialog();
        }

        private void BtnStats_Click(object sender, EventArgs e)
        {
            // Отваряме малко диалогово прозорче с избор
            var result = MessageBox.Show("Да отворя ли Класирането?\n(Yes = Класиране, No = Голмайстори)", "Избор на статистика", MessageBoxButtons.YesNoCancel);

            if (result == DialogResult.Yes)
            {
                new StandingsForm().ShowDialog();
            }
            else if (result == DialogResult.No)
            {
                new StatsForm().ShowDialog();
            }
        }

        // При затваряне на менюто, спираме цялото приложение
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e); // Запазва стандартното поведение
            Environment.Exit(0);  // <-- ТОВА Е КЛЮЧЪТ! Убива целия процес веднага.
        }
    }
}