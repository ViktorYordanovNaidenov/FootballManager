using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class MatchResultForm : Form
    {
        private int matchId;
        private int homeClubId, awayClubId;
        
        private Label lblScore;
        private ComboBox comboHomePlayers, comboAwayPlayers;
        private NumericUpDown numMinute;
        private ListBox listEvents; // Тук ще показваме кой е вкарал

        public MatchResultForm(int mId, string homeName, string awayName)
        {
            Theme.Apply(this);
            InitializeComponent();
            this.matchId = mId;
            this.Text = $"{homeName} vs {awayName}";
            
            // Първо намираме ID-тата на отборите, за да заредим играчите им
            GetClubIds();
            
            SetupUI(homeName, awayName);
            LoadPlayers();
            LoadExistingEvents(); // Ако вече има въведени голове
        }

        private void GetClubIds()
        {
            using (SqlConnection conn = Db.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT HomeClubId, AwayClubId FROM Matches WHERE MatchId=@mid", conn);
                cmd.Parameters.AddWithValue("@mid", matchId);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        homeClubId = r.GetInt32(0);
                        awayClubId = r.GetInt32(1);
                    }
                }
            }
        }

        private void SetupUI(string home, string away)
        {
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            lblScore = new Label() { Text = "0 : 0", Location = new Point(230, 20), Font = new Font("Arial", 20, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblScore);

            // ЛЯВО (Домакин)
            Label lblH = new Label() { Text = home, Location = new Point(20, 70), Font = new Font("Arial", 10, FontStyle.Bold) };
            this.Controls.Add(lblH);

            comboHomePlayers = new ComboBox() { Location = new Point(20, 95), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(comboHomePlayers);

            Button btnGoalHome = new Button() { Text = "ГОЛ +", Location = new Point(230, 93), Width = 60, BackColor = Color.LightGreen };
            btnGoalHome.Click += (s, e) => AddGoal(homeClubId, comboHomePlayers);
            this.Controls.Add(btnGoalHome);

            // ДЯСНО (Гост)
            Label lblA = new Label() { Text = away, Location = new Point(350, 70), Font = new Font("Arial", 10, FontStyle.Bold) };
            this.Controls.Add(lblA);

            comboAwayPlayers = new ComboBox() { Location = new Point(350, 95), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(comboAwayPlayers);

            Button btnGoalAway = new Button() { Text = "+ ГОЛ", Location = new Point(300, 93), Width = 60, BackColor = Color.LightGreen };
            btnGoalAway.Click += (s, e) => AddGoal(awayClubId, comboAwayPlayers);
            this.Controls.Add(btnGoalAway);

            // Минута
            Label lblMin = new Label() { Text = "Минута:", Location = new Point(20, 140), AutoSize = true };
            this.Controls.Add(lblMin);
            numMinute = new NumericUpDown() { Location = new Point(80, 138), Width = 60, Minimum = 1, Maximum = 120, Value = 1 };
            this.Controls.Add(numMinute);

            // Списък със събития
            listEvents = new ListBox() { Location = new Point(20, 180), Size = new Size(540, 200) };
            this.Controls.Add(listEvents);

            Button btnClose = new Button() { Text = "ЗАТВОРИ", Location = new Point(200, 400), Width = 200, Height = 40 };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void LoadPlayers()
        {
            using (SqlConnection conn = Db.GetConnection())
            {
                // Зареждаме домакините
                SqlDataAdapter da1 = new SqlDataAdapter($"SELECT PlayerId, CONCAT(LastName, ' (', Position, ')') as Name FROM Players WHERE CurrentClubId={homeClubId} AND IsActive=1", conn);
                DataTable dt1 = new DataTable(); da1.Fill(dt1);
                comboHomePlayers.DataSource = dt1; comboHomePlayers.DisplayMember = "Name"; comboHomePlayers.ValueMember = "PlayerId";

                // Зареждаме гостите
                SqlDataAdapter da2 = new SqlDataAdapter($"SELECT PlayerId, CONCAT(LastName, ' (', Position, ')') as Name FROM Players WHERE CurrentClubId={awayClubId} AND IsActive=1", conn);
                DataTable dt2 = new DataTable(); da2.Fill(dt2);
                comboAwayPlayers.DataSource = dt2; comboAwayPlayers.DisplayMember = "Name"; comboAwayPlayers.ValueMember = "PlayerId";
            }
        }

        private void AddGoal(int clubId, ComboBox combo)
        {
            if (combo.SelectedValue == null) return;
            int playerId = (int)combo.SelectedValue;
            int min = (int)numMinute.Value;

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // 1. Добавяме запис в таблица GOALS
                    string sqlGoal = "INSERT INTO Goals (MatchId, PlayerId, Minute) VALUES (@mid, @pid, @min)";
                    SqlCommand cmd = new SqlCommand(sqlGoal, conn);
                    cmd.Parameters.AddWithValue("@mid", matchId);
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    cmd.Parameters.AddWithValue("@min", min);
                    cmd.ExecuteNonQuery();

                    // 2. Обновяване на резултата в MATCHES (Увеличаваме с 1)
                    string col = (clubId == homeClubId) ? "HomeGoals" : "AwayGoals";
                    string sqlUpdate = $"UPDATE Matches SET {col} = {col} + 1, IsPlayed = 1 WHERE MatchId = {matchId}";
                    new SqlCommand(sqlUpdate, conn).ExecuteNonQuery();

                    MessageBox.Show("Голът е добавен!");
                    LoadExistingEvents(); // Презареждаме екрана
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadExistingEvents()
        {
            // Обновяваме резултата горе
            using (SqlConnection conn = Db.GetConnection())
            {
                SqlCommand cmd = new SqlCommand("SELECT HomeGoals, AwayGoals FROM Matches WHERE MatchId=@mid", conn);
                cmd.Parameters.AddWithValue("@mid", matchId);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    if (r.Read()) lblScore.Text = $"{r[0]} : {r[1]}";
                }
            }

            // Обновяваме списъка долу (кой в коя минута е вкарал)
            listEvents.Items.Clear();
            using (SqlConnection conn = Db.GetConnection())
            {
                string sql = @"
                    SELECT g.Minute, p.LastName, c.Name 
                    FROM Goals g 
                    JOIN Players p ON g.PlayerId = p.PlayerId 
                    JOIN Players p2 ON g.PlayerId = p2.PlayerId 
                    LEFT JOIN Clubs c ON p2.CurrentClubId = c.ClubId
                    WHERE g.MatchId = @mid 
                    ORDER BY g.Minute";
                
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@mid", matchId);
                using (SqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        listEvents.Items.Add($"Min {r[0]}: {r[1]} ({r[2]})");
                    }
                }
            }
        }
    }
}