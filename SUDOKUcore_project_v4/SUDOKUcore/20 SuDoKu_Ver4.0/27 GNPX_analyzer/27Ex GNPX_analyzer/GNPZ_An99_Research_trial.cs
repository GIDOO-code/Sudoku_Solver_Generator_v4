using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    public class Research_trial: AnalyzerBaseV2{

        // This function is for developing new Sudoku algorithms. Not included in standard GNPX applications.
        // To use this function, set "Research_" to the conditional compilation symbol field.

        // The world's hardest sudoku
        //  8..........36......7..9.2...5...7.......457.....1...3...1....68..85...1..9....4..


        public Research_trial( GNPX_AnalyzerMan AnMan ): base(AnMan){ }

        private int[]   Sol = new int[81];
        private int[][] BDX = new int[9][];
        private int[]   RowF=new int[9], ColF=new int[9], BlkF=new int[9];
        private  List<int>[]  RowPosLst = new List<int>[9];    //Undetermined cells position in row
        private  List<int>[]  RowNumLst = new List<int>[9];    //Undetermined numbers in row
        public bool TrialAndErrorApp( ){
            for( int k=0; k<9 ; k++ ){
                RowPosLst[k] = new List<int>();
                RowNumLst[k] = new List<int>();
            }

            for( int rc=0; rc<81; rc++ ){
                UCell P = pBOARD[rc];
                if( P.No !=0 )  Sol[P.rc] = Abs(P.No);
                else{
                    RowF[rc/9] |= P.FreeB;
                    ColF[rc%9] |= P.FreeB;
                    BlkF[rc.ToBlock()] |= P.FreeB;
                    RowPosLst[rc/9].Add(rc);
                }
            }

            for( int k=0; k<9; k++ ){
                foreach( var p in RowF[k].IEGet_BtoNo() )  RowNumLst[k].Add(p);
            }
            Set_RowLine( 0, RowF, ColF, BlkF );
            
            return (SolCode>0);
        }

        private bool Set_RowLine( int rowNo, int[] RowF0, int[] ColF0, int[] BlkF0 ){
            // Check_sol( rowNo, RowF0, ColF0, BlkF0 );

            if( rowNo == 9 ){   // ... found
                Check_sol( rowNo, RowF0, ColF0, BlkF0);//, Sol0 );

                SolCode=1;
                Result = "Trial And Error";
                ResultLong = Result;
                pBOARD.ForEach( P => { if( Sol[P.rc]<0 )  P.FixedNo=-Sol[P.rc]; } );

                if( __SimpleAnalyzerB__ )  return true;
                pAnMan.SnapSaveGP(pPZL);
                return true;
            }

            int nc = RowF[rowNo].BitCount();                        //number of blanks in row
            int[]   ColF1=new int[9], BlkF1=new int[9];

            if( nc > 0 ){
                Permutation prmX = new Permutation(nc);
                int nxt=9;
                while( prmX.Successor(nxt) ){
                    nxt = 9;
        
                    for( int k=0; k<9 ; k++ ){ ColF1[k]=ColF0[k]; BlkF1[k]=BlkF0[k]; }

                    for( int k=0; k<nc; k++ ){
                        int rc = RowPosLst[rowNo][k], c=rc%9, b=rc.ToBlock();
                        int no = RowNumLst[rowNo][ prmX.Index[k] ];
                        int noB = 1 << no;
                        if( (ColF1[c]&noB) == 0 ){ nxt=k; goto L_next_prmX;; }
                        if( (BlkF1[b]&noB) == 0 ){ nxt=k; goto L_next_prmX; }
                        ColF1[c] = ColF1[c].DifSet(noB);
                        BlkF1[b] = BlkF1[b].DifSet(noB);  
                        Sol[rc] = -(no+1);
                    }

                    bool ret0 = Set_RowLine( rowNo+1, RowF0, ColF1, BlkF1 );
                  //for( int k=0; k<nc; k++ )  Sol[ RowPosLst[rowNo][k] ] = 0;
                 L_next_prmX:
                    continue;
                }
                return false;
            }

             bool ret1 = Set_RowLine( rowNo+1, RowF0, ColF1, BlkF1 );

            return (rowNo==0 && SolCode>0);
        }

        private int solcc = 0;
        private void Check_sol( int rowNo, int[] RowF0, int[] ColF0, int[] BlkF0 ){
            solcc++;
            if( rowNo <= 7 )  return;
            string st = $"** rowNo:{rowNo} solcc:{solcc}";

         // for( int k=0; k<9; k++ )  st += $"\rfree k:{k}  r:{RowF0[k].ToBSt()}  c:{ColF0[k].ToBSt()}  b:{BlkF0[k].ToBSt()} "; 
            for( int rc=0; rc<81; rc++ ){
                if( rc%9 == 0 )  st += $"\r  {rc/9}: ";
                int s = Sol[rc];
                st += (s>0)? $"  {s}": (s<0)? $" {s}": "  .";
            }
            WriteLine( st );

        }

    }
}