using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq; // Важно за работа със списъци
using System.Windows.Forms;

namespace FootballManager
{
    public partial class MatchesForm : Form
    {
        private ComboBox comboLeagues;
        private Button btnGenerate, btnView;
        private DataGridView gridMatches;
        private Label lblStatus;

        public MatchesForm()
        {
            InitializeComponent();
            SetupUI();
            LoadLeagues();
            Theme.Apply(this);

        }

        private void SetupUI()
        {
            this.Text = "Програма на първенството";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1. Етикет
            Label lbl = new Label() { Text = "Избери Лига:", Location = new Point(20, 20), AutoSize = true };
            this.Controls.Add(lbl);

            // 2. Падащо меню (с добавка за автоматично зареждане)
            comboLeagues = new ComboBox() { Location = new Point(120, 17), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            // ВАЖНО: Щом сменим лигата, веднага зареждаме мачовете!
            comboLeagues.SelectedIndexChanged += (s, e) =>
            {
                if (comboLeagues.SelectedValue != null)
                {
                    int id = Convert.ToInt32(comboLeagues.SelectedValue);
                    LoadMatches(id);
                }
            };
            this.Controls.Add(comboLeagues);

            // 3. Бутон за генериране
            btnGenerate = new Button() { Text = "ГЕНЕРИРАЙ ПРОГРАМА", Location = new Point(340, 15), Width = 180, BackColor = Color.LightBlue };
            btnGenerate.Click += BtnGenerate_Click;
            this.Controls.Add(btnGenerate);

            // 4. Предупреждение
            lblStatus = new Label() { Text = "Внимание: Генерирането изтрива старата програма!", Location = new Point(20, 50), AutoSize = true, ForeColor = Color.Red };
            this.Controls.Add(lblStatus);

            // 5. Таблица (с добавка за двоен клик)
            gridMatches = new DataGridView()
            {
                Location = new Point(20, 80),
                Size = new Size(740, 450),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };

            // ВАЖНО: При двоен клик отваряме формата за резултат
            gridMatches.CellDoubleClick += GridMatches_CellDoubleClick;

            this.Controls.Add(gridMatches);
        }

        private void LoadLeagues()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT LeagueId, Name FROM Leagues", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    comboLeagues.DataSource = dt;
                    comboLeagues.DisplayMember = "Name";
                    comboLeagues.ValueMember = "LeagueId";
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // --- ТУК Е МАГИЯТА (АЛГОРИТЪМЪТ) ---
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (comboLeagues.SelectedValue == null) return;
            int leagueId = Convert.ToInt32(comboLeagues.SelectedValue);

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // 1. Взимаме всички отбори в лигата
                    List<int> teamIds = new List<int>();
                    SqlCommand cmdGetTeams = new SqlCommand("SELECT ClubId FROM LeagueTeams WHERE LeagueId = @lid", conn);
                    cmdGetTeams.Parameters.AddWithValue("@lid", leagueId);

                    using (SqlDataReader reader = cmdGetTeams.ExecuteReader())
                    {
                        while (reader.Read()) teamIds.Add(reader.GetInt32(0));
                    }

                    if (teamIds.Count < 2)
                    {
                        MessageBox.Show("Трябват поне 2 отбора за програма!"); return;
                    }

                    // Ако са нечетен брой, добавяме "служебен" отбор (0), който означава "почивка"
                    if (teamIds.Count % 2 != 0)
                    {
                        teamIds.Add(0);
                    }

                    int numTeams = teamIds.Count;
                    int numRounds = (numTeams - 1) * 2; // Умножаваме по 2 за реванши (Home & Away)
                    int matchesPerRound = numTeams / 2;

                    // 2. Изтриваме старата програма за тази лига (за да не се дублира)
                    SqlCommand cmdDelete = new SqlCommand("DELETE FROM Matches WHERE LeagueId = @lid", conn);
                    cmdDelete.Parameters.AddWithValue("@lid", leagueId);
                    cmdDelete.ExecuteNonQuery();

                    // 3. Генериране на кръговете (Round Robin Algorithm)
                    for (int round = 0; round < numRounds; round++)
                    {
                        for (int match = 0; match < matchesPerRound; match++)
                        {
                            int home = (round + match) % (numTeams - 1);
                            int away = (numTeams - 1 - match + round) % (numTeams - 1);

                            // Последната позиция се върти специално
                            if (match == 0) away = numTeams - 1;

                            int homeTeamId = teamIds[home];
                            int awayTeamId = teamIds[away];

                            // Разменяме домакинството във втората половина на сезона (реванши)
                            if (round >= numTeams - 1)
                            {
                                int temp = homeTeamId;
                                homeTeamId = awayTeamId;
                                awayTeamId = temp;
                            }

                            // Ако някой от отборите е 0 (служебния), значи другият почива -> не записваме мач
                            if (homeTeamId != 0 && awayTeamId != 0)
                            {
                                string insertSql = @"
                                    INSERT INTO Matches (LeagueId, HomeClubId, AwayClubId, MatchDate, RoundNumber, IsPlayed) 
                                    VALUES (@lid, @h, @a, @date, @r, 0)";

                                SqlCommand cmdInsert = new SqlCommand(insertSql, conn);
                                cmdInsert.Parameters.AddWithValue("@lid", leagueId);
                                cmdInsert.Parameters.AddWithValue("@h", homeTeamId);
                                cmdInsert.Parameters.AddWithValue("@a", awayTeamId);
                                cmdInsert.Parameters.AddWithValue("@r", round + 1); // Кръговете почват от 1
                                // Слагаме примерна дата (всяка седмица нов кръг)
                                cmdInsert.Parameters.AddWithValue("@date", DateTime.Now.AddDays((round + 1) * 7));

                                cmdInsert.ExecuteNonQuery();
                            }
                        }
                    }

                    MessageBox.Show("Програмата е генерирана успешно!");
                    LoadMatches(leagueId);
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка при генериране: " + ex.Message); }
        }

        // Показване на мачовете в таблицата
        private void LoadMatches(int leagueId)
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    string sql = @"
                        SELECT m.MatchId, m.RoundNumber as 'Кръг', 
                               hc.Name as 'Домакин', 
                               ac.Name as 'Гост', 
                               m.MatchDate as 'Дата',
                               CASE WHEN m.IsPlayed = 1 THEN CONCAT(m.HomeGoals, ' - ', m.AwayGoals) ELSE '- : -' END as 'Резултат'
                        FROM Matches m
                        JOIN Clubs hc ON m.HomeClubId = hc.ClubId
                        JOIN Clubs ac ON m.AwayClubId = ac.ClubId
                        WHERE m.LeagueId = @lid
                        ORDER BY m.RoundNumber, m.MatchDate";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@lid", leagueId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridMatches.DataSource = dt;
                    gridMatches.Columns["MatchId"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void GridMatches_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = gridMatches.Rows[e.RowIndex];

                // Взимаме данните от избрания ред
                int matchId = Convert.ToInt32(row.Cells["MatchId"].Value);
                string home = row.Cells["Домакин"].Value.ToString();
                string away = row.Cells["Гост"].Value.ToString();

                // Парсваме текущия резултат (ако има такъв)
                string score = row.Cells["Резултат"].Value.ToString(); // Пример: "2 - 1" или "- : -"
                int hGoals = 0, aGoals = 0;

                if (score.Contains("-") && !score.Contains(":"))
                {
                    var parts = score.Split('-');
                    int.TryParse(parts[0].Trim(), out hGoals);
                    int.TryParse(parts[1].Trim(), out aGoals);
                }

                // Отваряме формата за резултат
                // Вече подаваме само ID и имената. Резултатът си го дърпаме от базата.
                MatchResultForm form = new MatchResultForm(matchId, home, away);
                form.ShowDialog();

                // След като я затворим, обновяваме таблицата, за да видим новия резултат
                if (comboLeagues.SelectedValue is int lid) LoadMatches(lid);
            }
        }
    }
}