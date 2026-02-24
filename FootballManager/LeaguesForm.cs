using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class LeaguesForm : Form
    {
        // Лява част (Лиги)
        private DataGridView gridLeagues;
        private TextBox txtName, txtSeason;
        private Button btnAddLeague, btnDeleteLeague;

        // Дясна част (Отбори в избраната лига)
        private DataGridView gridLeagueTeams;
        private ComboBox comboClubs;
        private Button btnAddTeamToLeague, btnRemoveTeamFromLeague;
        private Label lblSelectedLeague;

        private int selectedLeagueId = 0;

        public LeaguesForm()
        {
            InitializeComponent();
            SetupUI();
            LoadLeagues();
            LoadAllClubs();
            Theme.Apply(this);
        }

        private void SetupUI()
        {
            this.Text = "Управление на Първенства";
            this.Size = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // --- ЛЯВА ЧАСТ: ЛИГИ ---
            Label lblL = new Label() { Text = "1. Създай Лига:", Location = new Point(20, 20), Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblL);

            this.Controls.Add(new Label() { Text = "Име:", Location = new Point(20, 50), AutoSize = true });
            txtName = new TextBox() { Location = new Point(20, 70), Width = 150 };
            this.Controls.Add(txtName);

            this.Controls.Add(new Label() { Text = "Сезон:", Location = new Point(180, 50), AutoSize = true });
            txtSeason = new TextBox() { Location = new Point(180, 70), Width = 100 };
            this.Controls.Add(txtSeason);

            btnAddLeague = new Button() { Text = "СЪЗДАЙ", Location = new Point(290, 68), Width = 80, BackColor = Color.LightGreen };
            btnAddLeague.Click += BtnAddLeague_Click;
            this.Controls.Add(btnAddLeague);

            gridLeagues = new DataGridView() { Location = new Point(20, 110), Size = new Size(350, 320), ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false };
            gridLeagues.CellClick += GridLeagues_CellClick;
            this.Controls.Add(gridLeagues);

            btnDeleteLeague = new Button() { Text = "Изтрий Лига", Location = new Point(20, 440), Width = 100, BackColor = Color.Salmon, Enabled = false };
            btnDeleteLeague.Click += BtnDeleteLeague_Click;
            this.Controls.Add(btnDeleteLeague);


            // --- ДЯСНА ЧАСТ: ОТБОРИ ---
            Label lblR = new Label() { Text = "2. Добави отбори в лигата:", Location = new Point(400, 20), Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblR);

            lblSelectedLeague = new Label() { Text = "Избери лига отляво...", Location = new Point(400, 50), ForeColor = Color.Blue, AutoSize = true };
            this.Controls.Add(lblSelectedLeague);

            comboClubs = new ComboBox() { Location = new Point(400, 80), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            this.Controls.Add(comboClubs);

            btnAddTeamToLeague = new Button() { Text = "ДОБАВИ ОТБОР", Location = new Point(610, 78), Width = 120, Enabled = false };
            btnAddTeamToLeague.Click += BtnAddTeamToLeague_Click;
            this.Controls.Add(btnAddTeamToLeague);

            gridLeagueTeams = new DataGridView() { Location = new Point(400, 110), Size = new Size(450, 320), ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AllowUserToAddRows = false };
            this.Controls.Add(gridLeagueTeams);

            btnRemoveTeamFromLeague = new Button() { Text = "Махни отбор", Location = new Point(400, 440), Width = 100, ForeColor = Color.Red, Enabled = false };
            btnRemoveTeamFromLeague.Click += BtnRemoveTeamFromLeague_Click;
            this.Controls.Add(btnRemoveTeamFromLeague);
        }

        // --- ЛОГИКА ЗА ЛИГИТЕ ---

        private void LoadLeagues()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Leagues", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridLeagues.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void BtnAddLeague_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtSeason.Text))
            {
                MessageBox.Show("Въведи име и сезон!"); return;
            }

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlCommand cmd = new SqlCommand("INSERT INTO Leagues (Name, Season) VALUES (@n, @s)", conn);
                    cmd.Parameters.AddWithValue("@n", txtName.Text);
                    cmd.Parameters.AddWithValue("@s", txtSeason.Text);
                    cmd.ExecuteNonQuery();

                    txtName.Clear(); txtSeason.Clear();
                    LoadLeagues();
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void GridLeagues_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < gridLeagues.Rows.Count)
            {
                var row = gridLeagues.Rows[e.RowIndex];
                if (row.Cells["LeagueId"].Value == DBNull.Value) return;

                selectedLeagueId = Convert.ToInt32(row.Cells["LeagueId"].Value);
                string leagueName = row.Cells["Name"].Value.ToString();

                lblSelectedLeague.Text = $"Избрана лига: {leagueName}";

                // Активираме дясната част
                comboClubs.Enabled = true;
                btnAddTeamToLeague.Enabled = true;
                btnDeleteLeague.Enabled = true;
                btnRemoveTeamFromLeague.Enabled = true;

                LoadLeagueTeams();
            }
        }

        private void BtnDeleteLeague_Click(object sender, EventArgs e)
        {
            if (selectedLeagueId == 0) return;
            if (MessageBox.Show("Сигурен ли си? Това ще изтрие и всички мачове в лигата!", "Внимание", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = Db.GetConnection())
                    {
                        // Първо трием връзките
                        new SqlCommand($"DELETE FROM LeagueTeams WHERE LeagueId={selectedLeagueId}", conn).ExecuteNonQuery();
                        // После лигата
                        new SqlCommand($"DELETE FROM Leagues WHERE LeagueId={selectedLeagueId}", conn).ExecuteNonQuery();

                        LoadLeagues();
                        gridLeagueTeams.DataSource = null;
                        selectedLeagueId = 0;
                    }
                }
                catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
            }
        }

        // --- ЛОГИКА ЗА ОТБОРИТЕ В ЛИГАТА ---

        private void LoadAllClubs()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT ClubId, Name FROM Clubs ORDER BY Name", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    comboClubs.DataSource = dt;
                    comboClubs.DisplayMember = "Name";
                    comboClubs.ValueMember = "ClubId";
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void LoadLeagueTeams()
        {
            if (selectedLeagueId == 0) return;
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Взимаме имената на отборите, които са записани в тази лига
                    string sql = @"
                        SELECT lt.LeagueTeamId, c.Name as ClubName, c.City, c.Stadium 
                        FROM LeagueTeams lt
                        JOIN Clubs c ON lt.ClubId = c.ClubId
                        WHERE lt.LeagueId = @lid";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.Parameters.AddWithValue("@lid", selectedLeagueId);

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridLeagueTeams.DataSource = dt;
                    gridLeagueTeams.Columns["LeagueTeamId"].Visible = false;
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void BtnAddTeamToLeague_Click(object sender, EventArgs e)
        {
            if (selectedLeagueId == 0 || comboClubs.SelectedValue == null) return;

            int clubId = Convert.ToInt32(comboClubs.SelectedValue);

            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Проверка за дублиране (да не добавим отбора два пъти)
                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM LeagueTeams WHERE LeagueId=@lid AND ClubId=@cid", conn);
                    checkCmd.Parameters.AddWithValue("@lid", selectedLeagueId);
                    checkCmd.Parameters.AddWithValue("@cid", clubId);

                    int exists = (int)checkCmd.ExecuteScalar();
                    if (exists > 0)
                    {
                        MessageBox.Show("Този отбор вече е в лигата!");
                        return;
                    }

                    // Добавяне
                    SqlCommand cmd = new SqlCommand("INSERT INTO LeagueTeams (LeagueId, ClubId) VALUES (@lid, @cid)", conn);
                    cmd.Parameters.AddWithValue("@lid", selectedLeagueId);
                    cmd.Parameters.AddWithValue("@cid", clubId);
                    cmd.ExecuteNonQuery();

                    LoadLeagueTeams();
                }
            }
            catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
        }

        private void BtnRemoveTeamFromLeague_Click(object sender, EventArgs e)
        {
            if (gridLeagueTeams.SelectedRows.Count > 0)
            {
                int ltId = Convert.ToInt32(gridLeagueTeams.SelectedRows[0].Cells["LeagueTeamId"].Value);
                try
                {
                    using (SqlConnection conn = Db.GetConnection())
                    {
                        new SqlCommand($"DELETE FROM LeagueTeams WHERE LeagueTeamId={ltId}", conn).ExecuteNonQuery();
                        LoadLeagueTeams();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Грешка: " + ex.Message); }
            }
        }
    }
}