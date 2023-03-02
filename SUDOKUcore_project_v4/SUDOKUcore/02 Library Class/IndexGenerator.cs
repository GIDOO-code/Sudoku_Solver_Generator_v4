using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    /*  for test    
    public class IndexGenerator{
        IndexGenerator(){}
        
        public void testStart(){
            List<string>[] strLst = new List<string>[3];
            strLst[0] = new List<string>{"a","b","c"};
            strLst[1] = new List<string>{"X","Y"};
            strLst[2] = new List<string>{"1","2","3","4"};
            foreach( var P in IEGet_ListIndex(strLst) ){ 
                string st="P:";
                foreach( var x in P )  st += $" {x}";
                WriteLine( st );
            }
        }

        public  IEnumerable<int[]> IE_IndexGenerater( List<string>[] strLst ){ 
            int sz = strLst.Length;
            int[] szArray = new int[sz];
            for( int k=0; k<sz; k++ )  szArray[k] = strLst[k].Count;

            int[] idxLst = new int[sz];
            yield return idxLst;
            int pos=sz-1, ix;
            while( pos>=0 ){ 
                if( ++idxLst[pos] < szArray[pos] ){ 
                    if( pos==sz-1 ){ yield return idxLst; }
                    else{ pos++; idxLst[pos]=-1; }
                }
                else{ pos--; }
            }
            yield break;            
        }
    }
    */
}
