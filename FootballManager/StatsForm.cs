using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class StatsForm : Form
    {
        private DataGridView gridStats;

        public StatsForm()
        {
            Theme.Apply(this);
            InitializeComponent();
            this.Text = "Статистика - Голмайстори";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label lbl = new Label() { Text = "ТОП ГОЛМАЙСТОРИ", Location = new Point(20, 20), Font = new Font("Arial", 14, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lbl);

            gridStats = new DataGridView()
            {
                Location = new Point(20, 60),
                Size = new Size(540, 380),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.Controls.Add(gridStats);

            LoadTopScorers();
        }

        private void LoadTopScorers()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Поправка: ORDER BY COUNT(g.GoalId) вместо името в кавички
                    string sql = @"
                SELECT TOP 20 
                       p.FirstName + ' ' + p.LastName as 'Играч',
                       c.Name as 'Клуб',
                       COUNT(g.GoalId) as 'Голове'
                FROM Goals g
                JOIN Players p ON g.PlayerId = p.PlayerId
                LEFT JOIN Clubs c ON p.CurrentClubId = c.ClubId
                GROUP BY p.PlayerId, p.FirstName, p.LastName, c.Name
                ORDER BY COUNT(g.GoalId) DESC";  // <-- ТУК БЕШЕ ГРЕШКАТА

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridStats.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}