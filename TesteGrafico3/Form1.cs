using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TesteGrafico3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;

            comboAno.SelectedIndexChanged += FiltrosAlterados;
            comboMes.SelectedIndexChanged += FiltrosAlterados;
            comboVisualizacao.SelectedIndexChanged += FiltrosAlterados;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PreencherFiltros();
            CarregarGrafico(DateTime.Now.Year, DateTime.Now.Month); // Padrão: Mensal
        }

        private void PreencherFiltros()
        {
            // Preencher meses com a primeira letra maiúscula
            comboMes.Items.Clear();
            var cultura = new CultureInfo("pt-BR");

            for (int i = 1; i <= 12; i++)
            {
                comboMes.Items.Add(Capitalizar(cultura.DateTimeFormat.GetMonthName(i)));
            }
            comboMes.SelectedIndex = DateTime.Now.Month - 1;

            // Preencher tipos de visualização
            comboVisualizacao.Items.Clear();
            comboVisualizacao.Items.AddRange(new string[] { "Mensal", "Anual" });
            comboVisualizacao.SelectedIndex = 0;

            // Preencher anos disponíveis no banco
            string query = "SELECT DISTINCT YEAR(data_vencimento) AS ano FROM conta ORDER BY ano DESC";

            try
            {
                using (var conn = CriarConexao())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    comboAno.Items.Clear();
                    while (reader.Read())
                        comboAno.Items.Add(reader.GetInt32("ano").ToString());

                    comboAno.SelectedItem = DateTime.Now.Year.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar filtros: " + ex.Message);
            }
        }

        private string Capitalizar(string texto)
        {
            return char.ToUpper(texto[0]) + texto.Substring(1); // Capitaliza a primeira letra
        }

        private void FiltrosAlterados(object sender, EventArgs e)
        {
            if (comboAno.SelectedItem == null || comboVisualizacao.SelectedItem == null) return;

            int ano = int.Parse(comboAno.SelectedItem.ToString());
            bool isAnual = comboVisualizacao.SelectedItem.ToString() == "Anual";
            int mes = isAnual ? 0 : comboMes.SelectedIndex + 1;

            CarregarGrafico(ano, mes);
        }

        private void CarregarGrafico(int ano, int mes)
        {
            var filtros = new Dictionary<string, object> { { "@ano", ano } };
            if (mes > 0) filtros.Add("@mes", mes);

            decimal totalDespesas = ObterTotalPorTipo("Despesa", filtros);
            decimal totalReceitas = ObterTotalPorTipo("Receita", filtros);
            decimal lucro = totalReceitas - totalDespesas;

            string nomeMes = mes > 0 ? Capitalizar(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mes)) : "";

            CriarGrafico(
                $"Despesas vs Receitas - {ano}" + (mes > 0 ? $" / {nomeMes}" : ""),
                new string[] { "Despesas", "Receitas" },
                new decimal[] { totalDespesas, totalReceitas },
                new Color[] { Color.Red, Color.Green },
                $"Lucro (Receita - Despesa): R$ {lucro:N2}"
            );
        }

        private decimal ObterTotalPorTipo(string tipo, Dictionary<string, object> filtros)
        {
            string query = "SELECT SUM(valor) AS total_valor FROM conta WHERE tipo = @tipo AND YEAR(data_vencimento) = @ano";
            if (filtros.ContainsKey("@mes"))
                query += " AND MONTH(data_vencimento) = @mes";

            using (var conn = CriarConexao())
            {
                conn.Open();
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tipo", tipo);
                foreach (var param in filtros)
                    cmd.Parameters.AddWithValue(param.Key, param.Value);

                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
            }
        }

        private void CriarGrafico(string titulo, string[] categorias, decimal[] valores, Color[] cores, string subtitulo)
        {
            chart1.Series.Clear();
            chart1.Titles.Clear();
            chart1.Legends.Clear();

            chart1.Titles.Add(titulo).Font = new Font("Arial", 14, FontStyle.Bold);
            chart1.Titles.Add(subtitulo).Font = new Font("Arial", 10, FontStyle.Regular);

            var series = new Series
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                LabelForeColor = Color.Black,
                Font = new Font("Arial", 9),
                Label = "#PERCENT{P0}"
            };

            for (int i = 0; i < categorias.Length; i++)
            {
                int idx = series.Points.AddXY(categorias[i], valores[i]);
                series.Points[idx].Color = cores[i];
                series.Points[idx].LegendText = $"{categorias[i]} - R$ {valores[i]:N2}";
            }

            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = "Black";
            series["PieStartAngle"] = "270";

            chart1.Series.Add(series);

            chart1.Legends.Add(new Legend
            {
                Docking = Docking.Right,
                Alignment = StringAlignment.Near,
                Font = new Font("Arial", 9)
            });
        }

        private MySqlConnection CriarConexao()
        {
            return new MySqlConnection("Server=localhost;Database=testesistema;Uid=root;Pwd=;");
        }
    }
}