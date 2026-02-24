using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient; // Тук ползваме библиотеката, която инсталира

namespace FootballManager
{
    public partial class Form1 : Form
    {
        // Дефинираме елементите на формата
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;

        public Form1()
        {
            InitializeComponent();
            SetupTestUI(); // Стартираме нашия метод за визуализация
            Theme.Apply(this);
        }

        // Този метод рисува формата без да ползва Designer-а
        private void SetupTestUI()
        {
            this.Text = "Football Manager - Вход";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Етикет за име
            Label lblUser = new Label() { Text = "Потребител:", Location = new Point(50, 50), AutoSize = true };
            this.Controls.Add(lblUser);

            // Поле за име
            txtUsername = new TextBox() { Location = new Point(150, 47), Width = 150 };
            this.Controls.Add(txtUsername);

            // Етикет за парола
            Label lblPass = new Label() { Text = "Парола:", Location = new Point(50, 100), AutoSize = true };
            this.Controls.Add(lblPass);

            // Поле за парола
            txtPassword = new TextBox() { Location = new Point(150, 97), Width = 150, PasswordChar = '*' };
            this.Controls.Add(txtPassword);

            // Бутон за вход
            btnLogin = new Button() { Text = "ВХОД", Location = new Point(150, 150), Width = 100, Height = 40 };
            btnLogin.Click += BtnLogin_Click; // Закачаме събитието (Click)
            this.Controls.Add(btnLogin);

            // Етикет за статус (ако има грешка)
            lblStatus = new Label() { Text = "", Location = new Point(50, 210), AutoSize = true, ForeColor = Color.Red };
            this.Controls.Add(lblStatus);
        }

        // Логиката при натискане на бутона
        // Логиката при натискане на бутона
        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Моля въведете име и парола!", "Грешка");
                return;
            }

            try
            {
                // Използваме класа Db, който направихме
                using (SqlConnection conn = Db.GetConnection())
                {
                    // Проверка в таблицата Users
                    string query = "SELECT Role FROM Users WHERE Username = @u AND PasswordHash = @p";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", username);
                    cmd.Parameters.AddWithValue("@p", password);

                    object result = cmd.ExecuteScalar(); // Връща първата клетка (Ролята) или null

                    if (result != null)
                    {
                        string role = result.ToString();

                        // Тук беше грешката - махаме излишните скоби
                        lblStatus.ForeColor = Color.Green;
                        lblStatus.Text = $"Успех! Роля: {role}";
                        MessageBox.Show($"Добре дошъл, {username}!\nТи си: {role}", "Успешен вход");
                        // Скриваме логин формата
                        this.Hide();

                        // Отваряме менюто и подаваме ролята (Admin/Operator)
                        MainMenuForm menu = new MainMenuForm(role);
                        menu.Show();
                    }
                    else
                    {
                        lblStatus.ForeColor = Color.Red;
                        lblStatus.Text = "Грешно име или парола.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Грешка с базата данни: " + ex.Message, "System Error");
            }
        }
    }
}