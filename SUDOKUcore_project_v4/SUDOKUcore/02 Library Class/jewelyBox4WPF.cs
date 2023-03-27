using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using static System.ValueTuple;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GIDOO_space{
    static public class JKT{
        static public DateTime stopWatchStart( ){ return DateTime.Now; }
        static public TimeSpan stopWatchEnd_TimeSpan( DateTime dtSTart ){
            DateTime dt=DateTime.Now;
            TimeSpan diff=dt.Subtract(dtSTart);
            return  diff;
        }
        static public double stopWatchEnd( DateTime dtSTart ){
            DateTime dt=DateTime.Now;
            TimeSpan diff=dt.Subtract(dtSTart);
            return  (double)(diff.Ticks/1.0e7);
        }
        static public Color ReverseColor( Color cr ){  //### unsolved ###
            byte R=(byte)(0xFF-cr.R);
            byte G=(byte)(0xFF-cr.G);
            byte B=(byte)(0xFF-cr.B);
            return Color.FromArgb( cr.A, R, G, B);
        }
       
        //===== Normal Distribution =====
        public static Random sysRndm=new Random(0);
        public static void 　randomSeedSet( int seed ){
            if( seed==0 ){
                DateTime dt=DateTime.Now;
                seed=(int)dt.Ticks % 10000000;
            }
            sysRndm=new Random( seed );
         }
        public static double _NormalDistribution( double average, double standDev ){
            double rn=_NormalDistribution()*standDev + average;
            return rn;
        }    
        public static double _NormalDistribution(){  //Generate a normal distribution random variable
            double ra = sysRndm.NextDouble();
            double rb = sysRndm.NextDouble();

            //Box-Muller transform
            double rNorm = Math.Sqrt(-2.0*Math.Log(ra)) * Math.Sin( 2.0*Math.PI*rb );
            return rNorm;
        }

        public static IEnumerable<(T,int)> WithIndex<T>(this IEnumerable<T> ts){
            return ts.Select((t,i) => (t,i));
        }
    }
}