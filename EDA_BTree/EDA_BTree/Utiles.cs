using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;

namespace EDA_BTree
{
    static class Estilos
    {
        static public readonly Duration duracionResaltado = TimeSpan.FromMilliseconds(100);

        static Dictionary<Control, Style> estiloAnterior = new Dictionary<Control, Style>();

        static public DropShadowEffect shadow = new DropShadowEffect()
        {
            BlurRadius = 3,
            ShadowDepth = 0,
            Opacity = 0.4
        };
        
        static public void SetStyle(Control e, string type)
        {
            if(!estiloAnterior.ContainsKey(e))
                estiloAnterior[e] = e.Style;
            e.Style = (Style)Application.Current.Resources[type];
        }

        static public void ResetStyle(Control e)
        {
            e.Style = estiloAnterior[e];
        }

        static public async Task highlight(Control e, string type = "Query")
        {
            SetStyle(e, type);
            await Task.Delay(duracionResaltado.TimeSpan);
            ResetStyle(e);
        }
    }

    static class Animaciones
    {
        static public readonly int duracionPausa = 300;
        static public readonly Duration slideDuracion = TimeSpan.FromMilliseconds(1000);
        static public readonly IEasingFunction EasingFunc = new SineEase() { EasingMode = EasingMode.EaseInOut };

        static public DoubleAnimation slideAnimacionF(double to, Duration? duration = null)
        {
            return new DoubleAnimation()
            {
                Duration = duration != null ? (Duration)duration : slideDuracion,
                EasingFunction = EasingFunc,
                To = to
            };
        }

        static public Task animateTo(UIElement e, DependencyProperty dp, double to)
        {
            var animation = slideAnimacionF(to);
            var tcs = new TaskCompletionSource<object>();
            animation.Completed += (sender, args) => tcs.SetResult(null);
            e.BeginAnimation(dp, animation);
            return tcs.Task;
        }

        static public Storyboard fadeIn(UIElement e)
        {
            e.Opacity = 0;

            var fadeInAnimation = new DoubleAnimation()
            {
                Duration = TimeSpan.FromSeconds(5),
                From = 0,
                To = 1,
                EasingFunction = EasingFunc
            };

            var sb = new Storyboard()
            {
                Duration = TimeSpan.FromSeconds(5),
            };

            sb.Children.Add(fadeInAnimation);

            Storyboard.SetTarget(fadeInAnimation, e);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));

            sb.Completed += (o, args) =>
            {
                Debug.WriteLine("Completado");
            };

            return sb;
        }
    }
}
