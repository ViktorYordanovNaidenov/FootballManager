using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq; // МНОГО ВАЖНО ЗА СОРТИРАНЕТО
using System.Windows.Forms;

namespace FootballManager
{
    public partial class StandingsForm : Form
    {
        private ComboBox comboLeagues;
        private DataGridView gridStandings;

        public StandingsForm()
        {
            InitializeComponent();
            SetupUI();
            LoadLeagues();
            Theme.Apply(this);
        }

        private void SetupUI()
        {
            this.Text = "Временно Класиране";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lbl = new Label() { Text = "Избери Лига:", Location = new Point(20, 20), AutoSize = true, Font = new Font("Arial", 12) };
            this.Controls.Add(lbl);

            comboLeagues = new ComboBox() { Location = new Point(140, 17), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Arial", 10) };
            comboLeagues.SelectedIndexChanged += (s, e) => CalculateStandings();
            this.Controls.Add(comboLeagues);

            gridStandings = new DataGridView()
            {
                Location = new Point(20, 60),
                Size = new Size(840, 480),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.Controls.Add(gridStandings);
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

        // --- ЛОГИКА ЗА ИЗЧИСЛЯВАНЕ ---
        private void CalculateStandings()
        {
            if (comboLeagues.SelectedValue == null) return;
            int leagueId = (int)comboLeagues.SelectedValue;

            // 1. Списък, в който ще пазим статистиката на всеки отбор
            Dictionary<int, TeamStats> stats = new Dictionary<int, TeamStats>();

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // А. Взимаме всички отбори в тази лига и ги добавяме в списъка с 0 точки
                    string sqlTeams = "SELECT c.ClubId, c.Name FROM LeagueTeams lt JOIN Clubs c ON lt.ClubId = c.ClubId WHERE lt.LeagueId = @lid";
                    SqlCommand cmdTeams = new SqlCommand(sqlTeams, conn);
                    cmdTeams.Parameters.AddWithValue("@lid", leagueId);

                    using (SqlDataReader r = cmdTeams.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = r.GetInt32(0);
                            string name = r.GetString(1);
                            stats[id] = new TeamStats { ClubName = name };
                        }
                    }

                    // Б. Взимаме всички ИЗИГРАНИ мачове (IsPlayed = 1)
                    string sqlMatches = "SELECT HomeClubId, AwayClubId, HomeGoals, AwayGoals FROM Matches WHERE LeagueId = @lid AND IsPlayed = 1";
                    SqlCommand cmdMatches = new SqlCommand(sqlMatches, conn);
                    cmdMatches.Parameters.AddWithValue("@lid", leagueId);

                    using (SqlDataReader r = cmdMatches.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int homeId = r.GetInt32(0);
                            int awayId = r.GetInt32(1);
                            int hGoals = r.GetInt32(2);
                            int aGoals = r.GetInt32(3);

                            if (!stats.ContainsKey(homeId) || !stats.ContainsKey(awayId)) continue; // Защита

                            // Обновяваме мачове и голове
                            stats[homeId].Played++;
                            stats[awayId].Played++;

                            stats[homeId].GF += hGoals; stats[homeId].GA += aGoals;
                            stats[awayId].GF += aGoals; stats[awayId].GA += hGoals;

                            // Обновяваме точки
                            if (hGoals > aGoals) // Домакинска победа
                            {
                                stats[homeId].Won++; stats[homeId].Points += 3;
                                stats[awayId].Lost++;
                            }
                            else if (aGoals > hGoals) // Гост победа
                            {
                                stats[awayId].Won++; stats[awayId].Points += 3;
                                stats[homeId].Lost++;
                            }
                            else // Равен
                            {
                                stats[homeId].Drawn++; stats[homeId].Points += 1;
                                stats[awayId].Drawn++; stats[awayId].Points += 1;
                            }
                        }
                    }
                }

                // В. Сортиране (Точки -> Голова разлика -> Вкарани голове)
                var sortedList = stats.Values
                    .OrderByDescending(t => t.Points)
                    .ThenByDescending(t => t.GD)
                    .ThenByDescending(t => t.GF)
                    .ToList();

                // Г. Добавяме номер (1, 2, 3...)
                int rank = 1;
                foreach (var team in sortedList) team.Position = rank++;

                // Д. Показваме в таблицата
                gridStandings.DataSource = sortedList;

                // Нагласяне на реда на колоните (за красота)
                gridStandings.Columns["Position"].HeaderText = "No";
                gridStandings.Columns["ClubName"].HeaderText = "Отбор";
                gridStandings.Columns["Played"].HeaderText = "М";
                gridStandings.Columns["Won"].HeaderText = "П";
                gridStandings.Columns["Drawn"].HeaderText = "Р";
                gridStandings.Columns["Lost"].HeaderText = "З";
                gridStandings.Columns["GF"].HeaderText = "ВГ"; // Вкарани
                gridStandings.Columns["GA"].HeaderText = "ДГ"; // Допуснати
                gridStandings.Columns["GD"].HeaderText = "ГР"; // Голова Разлика
                gridStandings.Columns["Points"].HeaderText = "ТОЧКИ";
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }
    }

    // Помощен клас за данните (слагаме го извън формата или най-отдолу в същия файл)
    public class TeamStats
    {
        public int Position { get; set; }
        public string ClubName { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GF { get; set; } // Goals For
        public int GA { get; set; } // Goals Against
        public int GD => GF - GA;   // Goal Difference (изчислява се автоматично)
        public int Points { get; set; }
    }
}