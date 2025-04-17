using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TesteGrafico2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);

            comboAno.SelectedIndexChanged += FiltrosAlterados;
            comboMes.SelectedIndexChanged += FiltrosAlterados;
            comboTipo.SelectedIndexChanged += FiltrosAlterados;
            comboVisualizacao.SelectedIndexChanged += FiltrosAlterados;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PreencherFiltros();

            int anoAtual = DateTime.Now.Year;
            int mesAtual = DateTime.Now.Month;
            string tipo = "Despesa";

            CarregarGraficoFiltrado(anoAtual, mesAtual, tipo); // Padrão: Mensal
        }

        private void PreencherFiltros()
        {
            // Tipo
            comboTipo.Items.Clear();
            comboTipo.Items.Add("Despesa");
            comboTipo.Items.Add("Receita");
            comboTipo.SelectedIndex = 0;

            // Mês
            comboMes.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                // Formatar o mês com a primeira letra maiúscula
                string nomeMes = new DateTime(1, i, 1).ToString("MMMM", new CultureInfo("pt-BR"));
                comboMes.Items.Add(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nomeMes.ToLower()));
            }
            comboMes.SelectedIndex = DateTime.Now.Month - 1;

            // Visualização (Mensal / Anual)
            comboVisualizacao.Items.Clear();
            comboVisualizacao.Items.Add("Mensal");
            comboVisualizacao.Items.Add("Anual");
            comboVisualizacao.SelectedIndex = 0;

            // Ano
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

                    while (reader.Read())
                    {
                        int ano = reader.GetInt32("ano");
                        comboAno.Items.Add(ano.ToString());
                    }

                    comboAno.SelectedItem = DateTime.Now.Year.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar filtros: " + ex.Message);
            }
        }


        private void FiltrosAlterados(object sender, EventArgs e)
        {
            if (comboAno.SelectedItem == null || comboTipo.SelectedItem == null || comboVisualizacao.SelectedItem == null)
                return;

            int ano = Convert.ToInt32(comboAno.SelectedItem);
            string tipo = comboTipo.SelectedItem.ToString();
            string visualizacao = comboVisualizacao.SelectedItem.ToString();

            if (visualizacao == "Anual")
            {
                CarregarGraficoAnual(ano, tipo);
            }
            else
            {
                if (comboMes.SelectedIndex == -1) return;
                int mes = comboMes.SelectedIndex + 1;
                CarregarGraficoFiltrado(ano, mes, tipo);
            }
        }

        private void CarregarGraficoFiltrado(int ano, int mes, string tipo)
        {
            string query = "SELECT categoria, SUM(valor) AS total_valor " +
                           "FROM conta " +
                           "WHERE YEAR(data_vencimento) = @ano AND MONTH(data_vencimento) = @mes AND tipo = @tipo " +
                           "GROUP BY categoria";

            GerarGrafico(query, ano, mes, tipo);
        }

        private void CarregarGraficoAnual(int ano, string tipo)
        {
            string query = "SELECT categoria, SUM(valor) AS total_valor " +
                           "FROM conta " +
                           "WHERE YEAR(data_vencimento) = @ano AND tipo = @tipo " +
                           "GROUP BY categoria";

            GerarGrafico(query, ano, 0, tipo, true);
        }

        private void GerarGrafico(string query, int ano, int mes, string tipo, bool isAnual = false)
        {
            using (MySqlConnection conn = new MySqlConnection("Server=localhost;Database=testesistema;Uid=root;Pwd=;"))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ano", ano);
                    if (!isAnual)
                        cmd.Parameters.AddWithValue("@mes", mes);
                    cmd.Parameters.AddWithValue("@tipo", tipo);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        chart1.Series.Clear();
                        chart1.Titles.Clear();
                        chart1.Legends.Clear();

                        string titulo;

                        if (isAnual)
                        {
                            titulo = $"{tipo}s por Categoria - {ano}";
                        }
                        else
                        {
                            // Formatar o mês com a primeira letra maiúscula
                            string nomeMes = new DateTime(ano, mes, 1).ToString("MMMM", new CultureInfo("pt-BR"));
                            nomeMes = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nomeMes.ToLower());  // Garantir a primeira letra maiúscula
                            titulo = $"{tipo}s por Categoria - {ano} {nomeMes}"; // Invertendo para o ano primeiro
                        }

                        // Adicionar título centralizado
                        chart1.Titles.Add(titulo).Font = new Font("Arial", 14, FontStyle.Bold);
                        chart1.Titles[0].Alignment = ContentAlignment.MiddleCenter; // Centralizando o título

                        Series pieSeries = new Series
                        {
                            ChartType = SeriesChartType.Pie,
                            IsValueShownAsLabel = true,
                            LabelForeColor = Color.Black,
                            Font = new Font("Arial", 9),
                            Label = "#PERCENT{P0}"
                        };

                        Dictionary<string, decimal> dados = new Dictionary<string, decimal>();

                        while (reader.Read())
                        {
                            string categoria = reader.IsDBNull(0) ? "Não Informada" : reader.GetString("categoria");
                            decimal valor = reader.GetDecimal("total_valor");
                            dados[categoria] = valor;
                        }

                        foreach (var item in dados)
                        {
                            int index = pieSeries.Points.AddXY(item.Key, item.Value);
                            pieSeries.Points[index].LegendText = $"{item.Key} - R$ {item.Value:N2}";
                        }

                        pieSeries["PieLabelStyle"] = "Outside";
                        pieSeries["PieLineColor"] = "Black";
                        pieSeries["PieStartAngle"] = "270";

                        chart1.Series.Add(pieSeries);

                        // Adicionar legenda à direita e centralizada verticalmente
                        chart1.Legends.Add(new Legend
                        {
                            Docking = Docking.Right, // Coloca a legenda à direita
                            Alignment = StringAlignment.Center, // Centraliza verticalmente
                            Font = new Font("Arial", 9)
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao carregar gráfico: " + ex.Message);
                }
            }
        }
    }
}