using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Text.RegularExpressions;

namespace EDA_BTree
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BTree V;

        public MainWindow()
        {
            InitializeComponent();

            V = new BTree(canvas);

            //var task = V.insert(5);
            //task.GetAwaiter().OnCompleted(async () =>
            //{
            //    await V.insert(2);
            //    await V.insert(11);
            //    await V.insert(7);
            //    await V.insert(9);
            //    await V.insert(4);
            //    await V.insert(0);
            //    await V.insert(15);
            //    await V.insert(3);
            //    await V.insert(1);
            //    await V.insert(-1);
            //    await V.insert(6);
            //    await V.insert(8);
            //    await V.insert(10);

            //    await V.insert(14);
            //});
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            V.raiz.redibujar();
        }

        private async void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            int number = int.Parse(txtEliminar.Text);
            string resultado = await V.Eliminar(number);
           lblResultado.Text += resultado + Environment.NewLine;
        }

        private async void Insertar_Click(object sender, RoutedEventArgs e)
        {
            int number = int.Parse(txtInsertar.Text);
            string resultado = await V.Insertar(number);
            lblResultado.Text += resultado + Environment.NewLine;
        }

        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            int number = int.Parse(txtBuscar.Text);
            string resultado = await V.Buscar(number);
            lblResultado.Text += resultado + Environment.NewLine;
        }

        private void Grado_TextChanged(object sender, TextChangedEventArgs e)
        {
            V = new BTree(canvas, int.Parse(txtGrado.Text));
        }

        private void Limpiar_Click(object sender, RoutedEventArgs e)
        {
            canvas.Children.Clear();
            V = new BTree(canvas);
        }

    }
}
