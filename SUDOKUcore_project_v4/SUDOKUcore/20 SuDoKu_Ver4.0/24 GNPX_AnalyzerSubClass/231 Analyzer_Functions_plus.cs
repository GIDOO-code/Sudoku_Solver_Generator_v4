using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Windows.Media.Animation;
//using Accessibility;
using System.Security.Policy;

namespace GNPXcore{

    // UInt128 is available in .net7.0.
    // Since int128 is implemented as a struct, Bit81A is not updated.
    //  ... Under development. Has bugs.

    static public class _Static_Function_for_Int128{            //UInt128 ver.
 
        static public int BitCount( this UInt128 arg ){
            int N = 0;
            UInt128 w = arg;
            for( int k=0; k<96; k+=32 ){
                        // WriteLine( $"w:{w.ToString81()}" );
                if( w != 0 )  N += ((uint)w).BitCount();
                w >>= 32;
            }
            return N;
        }

        static public string ToString81( this UInt128 arg81 ){
            string st="";
            
            for(int n=0; n<9; n++){
                int w = (int)arg81&0x1FF;
                st += w.ToBitString(9);
                if( n%3 == 2 )  st += " ";       
                if( n<8 ) st += " ";
                arg81 >>= 9;
            }
            return st;
        }

        static public UInt128 Get_rc_BitExpression( this List<UCell> aBOARD, int no=-1 ){
            UInt128 cells128=0;
            int noB = (no>=0)? (1<<no): 0;
            foreach( var p in aBOARD ){
                if( p.No != 0 )  continue; 
                if( (no>=0 && no<=8) && (p.FreeB&noB)==0 )  continue;
                cells128 |= (UInt128)1<<p.rc;   
            }
            return cells128;
        }

        static public IEnumerable<(int,bool)> IEGet_index_withFlag( this int bitRep, int length){
            int w = bitRep;
            for( int k=0; k<length; k++ ){
                yield return (k,(w&1)>0);
                w >>= 1;
            }
            yield break;

        }
        static public IEnumerable<(UCell,bool)> IEGet_cell_withFlag( this int bitRep, List<UCell> cells){
            int length = cells.Count;
            int w = bitRep;
            for( int k=0; k<length; k++ ){
                yield return (cells[k],(w&1)>0);
                w >>= 1;
            }
            yield break;

        }

        static public IEnumerable<int> IEGet_rc( this UInt128 bitRep ){
            int rc=0;
            UInt128 w=bitRep;
            do{
                if( (w&1) > 0 )  yield return rc; 
                w >>= 1;
            }while(w>0 && ++rc<81 );
            yield break;
        }

        static public IEnumerable<UCell> IEGet_Cells( this UInt128 bitRep, List<UCell> aBOARD ){
            int rc=0;
            UInt128 w=bitRep;
            do{
                if( (w&1) > 0 )  yield return aBOARD[rc]; 
                w >>= 1;
            }while(w>0 && ++rc<81 );
            yield break;
        }




    }  



}