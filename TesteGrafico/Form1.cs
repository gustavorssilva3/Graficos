using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TesteGrafico
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
            comboAno.SelectedIndexChanged += ComboAno_SelectedIndexChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CarregarAnosDisponiveis();
        }

        private void ComboAno_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboAno.SelectedItem != null)
            {
                int anoSelecionado = Convert.ToInt32(comboAno.SelectedItem.ToString());
                CarregarGrafico(anoSelecionado);
            }
        }

        private void CarregarAnosDisponiveis()
        {
            string connectionString = "Server=localhost;Database=testesistema;Uid=root;Pwd=;";
            string query = "SELECT DISTINCT YEAR(data_vencimento) AS ano FROM conta ORDER BY ano DESC";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    comboAno.Items.Clear();
                    int anoAtual = DateTime.Now.Year;
                    bool anoAtualExiste = false;

                    while (reader.Read())
                    {
                        int ano = reader.GetInt32("ano");
                        comboAno.Items.Add(ano.ToString());

                        if (ano == anoAtual)
                            anoAtualExiste = true;
                    }

                    if (anoAtualExiste)
                        comboAno.SelectedItem = anoAtual.ToString();
                    else if (comboAno.Items.Count > 0)
                        comboAno.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar anos: " + ex.Message);
            }
        }

        private void CarregarGrafico(int ano)
        {
            string connectionString = "Server=localhost;Database=testesistema;Uid=root;Pwd=;";
            string query = "SELECT MONTH(data_vencimento) AS mes, tipo, SUM(valor) AS total_valor " +
                           "FROM conta " +
                           "WHERE YEAR(data_vencimento) = @ano " +
                           "GROUP BY MONTH(data_vencimento), tipo " +
                           "ORDER BY MONTH(data_vencimento)";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ano", ano);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        chart1.Series.Clear();
                        chart1.Titles.Clear();
                        chart1.Legends.Clear();

                        chart1.Titles.Add("Rendimento Geral - " + ano).Font = new Font("Arial", 14, FontStyle.Bold);
                        chart1.Titles.Add("Tendências de receita para o ano selecionado").Font = new Font("Arial", 9, FontStyle.Regular);
                        chart1.Titles[1].Docking = Docking.Top;
                        chart1.Titles[1].ForeColor = Color.Gray;

                        var valoresPorMes = new Dictionary<int, (decimal receita, decimal despesa)>();
                        for (int i = 1; i <= 12; i++)
                            valoresPorMes[i] = (0, 0);

                        while (reader.Read())
                        {
                            int mes = reader.GetInt32("mes");
                            string tipo = reader.GetString("tipo");
                            decimal totalValor = reader.GetDecimal("total_valor");

                            if (tipo.ToLower() == "receita")
                                valoresPorMes[mes] = (valoresPorMes[mes].receita + totalValor, valoresPorMes[mes].despesa);
                            else if (tipo.ToLower() == "despesa")
                                valoresPorMes[mes] = (valoresPorMes[mes].receita, valoresPorMes[mes].despesa + totalValor);
                        }

                        //Formatação da coluna receita
                        Series receitaSeries = new Series("Receita")
                        {
                            ChartType = SeriesChartType.Column,
                            Color = Color.FromArgb(60, 179, 113),
                            IsValueShownAsLabel = false
                        };
                        receitaSeries["PointWidth"] = "0.6";

                        //Formatação da coluna despesa
                        Series despesaSeries = new Series("Despesa")
                        {
                            ChartType = SeriesChartType.Column,
                            Color = Color.FromArgb(255, 99, 132),
                            IsValueShownAsLabel = false
                        };
                        despesaSeries["PointWidth"] = "0.6";

                        foreach (var mes in valoresPorMes)
                        {
                            string nomeMes = GetMesNome(mes.Key) + "/" + (ano % 100).ToString("00");
                            receitaSeries.Points.AddXY(nomeMes, mes.Value.receita);
                            despesaSeries.Points.AddXY(nomeMes, mes.Value.despesa);
                        }

                        chart1.Series.Add(despesaSeries);
                        chart1.Series.Add(receitaSeries);

                        var chartArea = chart1.ChartAreas[0];
                        chartArea.AxisX.LabelStyle.Angle = 0;
                        chartArea.AxisX.Interval = 1;
                        chartArea.AxisX.MajorGrid.Enabled = false;
                        chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;

                        Legend legend = new Legend
                        {
                            Docking = Docking.Bottom,
                            Alignment = StringAlignment.Center
                        };
                        chart1.Legends.Add(legend);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao carregar dados: " + ex.Message);
                }
            }
        }

        private string GetMesNome(int mes)
        {
            string[] meses = { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
            return meses[mes - 1];
        }
    }
}
