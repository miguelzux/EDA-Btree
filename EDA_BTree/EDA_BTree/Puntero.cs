using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Input;

namespace EDA_BTree
{
    public class Puntero : TextBox
    {
        public static readonly int AnchoBorde = 2;
        public static readonly int AlturaDefecto = 30, AnchoDefecto = 15;

        private int indice;
        private Pagina paginaPadre = null;

        private bool actualizando;

        public bool Actualizando
        {
            get { return actualizando; }
            set { actualizando = value; }
        }

        public Pagina PaginaPadre
        {
            get { return paginaPadre; }
            set { paginaPadre = value; }
        }

        public bool existeNodoPocision
        {
            get
            {
                return (Pagina)Parent == paginaPadre
                    && Canvas.GetLeft(this) == distanciaIzquierda
                    && Canvas.GetTop(this) == distanciaArriba;
            }
        }

        public int Indice
        {
            get { return indice; }
            set
            {
                indice = value;
            }
        }

        public double distanciaIzquierda
        {
            get
            {
                return indice * (this.Width + Nodo.AnchoDefecto) - AnchoBorde * 2 * indice; ;
            }
        }

        public double distanciaArriba
        {
            get
            {
                return 0;
            }
        }

        public Task actualizar(bool animar = false)
        {
            var tcs = new TaskCompletionSource<object>();

            var task = tcs.Task;
            task.GetAwaiter().OnCompleted(() => {
                actualizando = false;
                this.SetValue(Panel.ZIndexProperty, DependencyProperty.UnsetValue);
            });

            if (existeNodoPocision || actualizando)
                tcs.SetResult(null);
            else
            {
                actualizando = true;
                Panel.SetZIndex(this, -1);
                var parent = (Canvas)Parent;

                if (parent != null)
                {
                    if (animar)
                    {
                        var pocision = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

                        if (double.IsNaN(pocision.X))
                            pocision.X = 0;
                        if (double.IsNaN(pocision.Y))
                            pocision.Y = 0;

                        var transformar = parent.TransformToVisual(paginaPadre);
                        pocision = transformar.Transform(pocision);
                        parent.Children.Remove(this);
                        paginaPadre.Children.Add(this);

                        Canvas.SetLeft(this, pocision.X);
                        Canvas.SetTop(this, pocision.Y);

                        var animacionArriba = Animaciones.slideAnimacionF(distanciaArriba);
                        var animacionIzquierda = Animaciones.slideAnimacionF(distanciaIzquierda);
                        animacionArriba.FillBehavior = animacionIzquierda.FillBehavior = FillBehavior.Stop;
                        animacionArriba.Completed += (s, e) =>
                        {
                            Canvas.SetTop(this, distanciaArriba);
                            tcs.SetResult(null);
                        };
                        animacionIzquierda.Completed += (s, e) =>
                        {
                            Canvas.SetLeft(this, distanciaIzquierda);
                        };
                        BeginAnimation(Canvas.LeftProperty, animacionIzquierda);
                        BeginAnimation(Canvas.TopProperty, animacionArriba);
                    }
                    else
                    {
                        Canvas.SetLeft(this, distanciaIzquierda);
                        Canvas.SetTop(this, distanciaArriba);
                        tcs.SetResult(null);
                    }
                }
                else
                {
                    PaginaPadre.Children.Add(this);
                    Canvas.SetLeft(this, distanciaIzquierda);
                    Canvas.SetTop(this, distanciaArriba);
                    if (animar)
                    {
                        Task.Delay(Animaciones.slideDuracion.TimeSpan)
                            .GetAwaiter()
                            .OnCompleted(() => tcs.SetResult(null));
                    }
                    else tcs.SetResult(null);
                }
            }

            return task;
        }

        public Puntero(int _index)
        {
            Focusable = false;
            Cursor = Cursors.Arrow;

            Width = AnchoDefecto; Height = AlturaDefecto;

            Style = (Style)Application.Current.Resources["TextBoxStyle"];

            indice = _index;
            Text = "●";

            Canvas.SetLeft(this, indice * (this.Width + Nodo.AnchoDefecto) - AnchoBorde * 2 * indice);
        }

        public async Task incrementoProgreso(int inc, bool animar = true)
        {
            indice += inc;
            await actualizar(true);
        }
    }
}
