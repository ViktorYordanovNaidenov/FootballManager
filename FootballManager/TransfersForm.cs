using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FootballManager
{
    public partial class TransfersForm : Form
    {
        private ComboBox comboPlayers, comboToClub;
        private TextBox txtCurrentClub, txtFee;
        private DataGridView gridHistory;
        private Button btnTransfer;

        // Помощна променлива да пазим сегашния клуб на избрания играч
        private int currentClubId = 0;

        public TransfersForm()
        {
            InitializeComponent();
            SetupUI();
            LoadPlayers();
            LoadClubs();
            LoadTransferHistory();
            Theme.Apply(this);
        }

        private void SetupUI()
        {
            this.Text = "Трансферен център";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Лява част - Извършване на трансфер
            Label lblTitle = new Label() { Text = "Нов Трансфер", Location = new Point(20, 20), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblTitle);

            Label lblP = new Label() { Text = "Избери Играч:", Location = new Point(20, 60), AutoSize = true };
            comboPlayers = new ComboBox() { Location = new Point(20, 85), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            comboPlayers.SelectedIndexChanged += ComboPlayers_SelectedIndexChanged; // Важно събитие
            this.Controls.Add(lblP); this.Controls.Add(comboPlayers);

            Label lblOld = new Label() { Text = "Текущ Отбор (От):", Location = new Point(20, 130), AutoSize = true };
            txtCurrentClub = new TextBox() { Location = new Point(20, 155), Width = 250, ReadOnly = true, BackColor = Color.WhiteSmoke };
            this.Controls.Add(lblOld); this.Controls.Add(txtCurrentClub);

            Label lblNew = new Label() { Text = "Нов Отбор (Към):", Location = new Point(20, 200), AutoSize = true };
            comboToClub = new ComboBox() { Location = new Point(20, 225), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(lblNew); this.Controls.Add(comboToClub);

            Label lblFee = new Label() { Text = "Трансферна сума (€):", Location = new Point(20, 270), AutoSize = true };
            txtFee = new TextBox() { Location = new Point(20, 295), Width = 150, Text = "0" };
            this.Controls.Add(lblFee); this.Controls.Add(txtFee);

            btnTransfer = new Button() { Text = "ПОДПИШИ", Location = new Point(20, 350), Width = 250, Height = 50, BackColor = Color.LightGreen, Font = new Font("Arial", 10, FontStyle.Bold) };
            btnTransfer.Click += BtnTransfer_Click;
            this.Controls.Add(btnTransfer);

            // Дясна част - История
            Label lblHist = new Label() { Text = "История на трансферите", Location = new Point(320, 20), Font = new Font("Arial", 12, FontStyle.Bold), AutoSize = true };
            this.Controls.Add(lblHist);

            gridHistory = new DataGridView()
            {
                Location = new Point(320, 60),
                Size = new Size(540, 480),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.Controls.Add(gridHistory);
        }

        private void LoadPlayers()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Взимаме играчите + ID-то на клуба им
                    string sql = "SELECT PlayerId, CONCAT(FirstName, ' ', LastName) as FullName, CurrentClubId FROM Players WHERE IsActive = 1 ORDER BY LastName";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    comboPlayers.DataSource = dt;
                    comboPlayers.DisplayMember = "FullName";
                    comboPlayers.ValueMember = "PlayerId";
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadClubs()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    SqlDataAdapter da = new SqlDataAdapter("SELECT ClubId, Name FROM Clubs ORDER BY Name", conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    comboToClub.DataSource = dt;
                    comboToClub.DisplayMember = "Name";
                    comboToClub.ValueMember = "ClubId";
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadTransferHistory()
        {
            try
            {
                using (SqlConnection conn = Db.GetConnection())
                {
                    string sql = @"
                        SELECT t.TransferDate as 'Дата',
                               CONCAT(p.FirstName, ' ', p.LastName) as 'Играч',
                               c1.Name as 'От',
                               c2.Name as 'Към',
                               t.TransferFee as 'Цена'
                        FROM Transfers t
                        JOIN Players p ON t.PlayerId = p.PlayerId
                        LEFT JOIN Clubs c1 ON t.FromClubId = c1.ClubId
                        JOIN Clubs c2 ON t.ToClubId = c2.ClubId
                        ORDER BY t.TransferDate DESC";

                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gridHistory.DataSource = dt;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        // Когато изберем играч, намираме сегашния му клуб
        private void ComboPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPlayers.SelectedItem is DataRowView row)
            {
                // Взимаме ID-то на сегашния клуб от скритото поле в Players DataRow
                var val = row["CurrentClubId"];

                if (val != DBNull.Value)
                {
                    currentClubId = Convert.ToInt32(val);
                    // Трябва да намерим името на този клуб
                    using (SqlConnection conn = Db.GetConnection())
                    {
                        string name = new SqlCommand($"SELECT Name FROM Clubs WHERE ClubId={currentClubId}", conn).ExecuteScalar().ToString();
                        txtCurrentClub.Text = name;
                    }
                }
                else
                {
                    currentClubId = 0;
                    txtCurrentClub.Text = "Свободен агент";
                }
            }
        }

        private void BtnTransfer_Click(object sender, EventArgs e)
        {
            if (comboPlayers.SelectedValue == null || comboToClub.SelectedValue == null)
            {
                MessageBox.Show("Избери играч и нов отбор!"); return;
            }

            int playerId = (int)comboPlayers.SelectedValue;
            int newClubId = (int)comboToClub.SelectedValue;
            decimal fee = 0;
            decimal.TryParse(txtFee.Text, out fee);

            // Валидация: Не може в същия клуб
            if (currentClubId == newClubId)
            {
                MessageBox.Show("Играчът вече е в този клуб! Избери друг.", "Грешка");
                return;
            }

            // ТРАНЗАКЦИЯ (Всичко или нищо)
            using (SqlConnection conn = Db.GetConnection())
            {
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.Transaction = transaction;

                    // 1. Запис в Историята (Transfers)
                    cmd.CommandText = @"INSERT INTO Transfers (PlayerId, FromClubId, ToClubId, TransferFee) 
                                        VALUES (@pid, @from, @to, @fee)";
                    cmd.Parameters.AddWithValue("@pid", playerId);
                    cmd.Parameters.AddWithValue("@from", currentClubId == 0 ? (object)DBNull.Value : currentClubId);
                    cmd.Parameters.AddWithValue("@to", newClubId);
                    cmd.Parameters.AddWithValue("@fee", fee);
                    cmd.ExecuteNonQuery();

                    // 2. Обновяване на Играча (Players)
                    cmd.CommandText = "UPDATE Players SET CurrentClubId = @newClub WHERE PlayerId = @pid2";
                    cmd.Parameters.AddWithValue("@newClub", newClubId);
                    cmd.Parameters.AddWithValue("@pid2", playerId);
                    cmd.ExecuteNonQuery();

                    // Ако всичко е наред - потвърждаваме
                    transaction.Commit();

                    MessageBox.Show("Трансферът е успешен!");
                    LoadTransferHistory(); // Обновяваме таблицата вдясно
                    LoadPlayers(); // Презареждаме играчите (за да се обнови CurrentClubId в паметта)

                    // Изчистване на полетата
                    txtCurrentClub.Clear();
                    txtFee.Text = "0";
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Ако гръмне нещо, връщаме всичко назад
                    MessageBox.Show("Грешка при трансфера: " + ex.Message);
                }
            }
        }
    }
}