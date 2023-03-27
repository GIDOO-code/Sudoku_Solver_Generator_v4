using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;

using GIDOO_space;
//using System.Windows.Input.Manipulations;
using static System.Diagnostics.Debug;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{

        //XY-Chain is an algorithm using Locked which occurs in the concatenation of bivalues.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page49.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //.5....3...71.43...2..61...9..5....7.7.34..19.1...9.8..3.2.64.5........185.927.4..
        //...71...9.14.9.....9....3.72...4.5.186.1.72......8.7....6471..5.......689.25..17.

        public bool XYChain(){
			Prepare();
			CeLKMan.PrepareCellLink(1);    //Generate StrongLink

            foreach( var (USol,noS,SolChain) in _GetXYChain_new() ){

                //===== XY-Chain found =====
                SolCode=2;                    
                String SolMsg = $"XY Chain {USol.rc.ToRCString()} #{noS+1} is false";
                Result = SolMsg;

                int noB = (1<<noS);
                USol.CancelB = noB; 
                USol.Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);  

                if( SolInfoB ){
                    string msg2 = "";
                    Color Cr = _ColorsLst[0];
                    foreach( var P in SolChain ){
                        P.UCe.Set_CellColorBkgColor_noBit(1<<P.no,AttCr,Cr);
                        msg2 += "-" + (P.UCe.rc).ToRCString();
                    }
                    ResultLong = SolMsg + "\n  > " + msg2.Substring(1);
                }
         
                    
                if( __SimpleAnalyzerB__ )  return true;
                if( !pAnMan.SnapSaveGP(pPZL) )  return true;
            }
            return false;
        }
                
        private IEnumerable<(UCell,int,List<ChainXY>)> _GetXYChain_new( ){
            Bit81 BP_bivalue = new Bit81(pBOARD,0x1FF,FreeBC:2);                      //bit representation of bivalue_cells
               
            var Que = new Queue<ChainXY>();
            List<ChainXY> ChainXYLst = new List<ChainXY>();

            Bit81[] Bpat = new Bit81[9];
            foreach( var PStart in BP_bivalue.IEGetUCell_noB(pBOARD,0x1FF) ){            //Choose one BV_Cell(=>PStart)
                int rcS = PStart.rc;
                Bit81 CnctdCs = ConnectedCells[rcS];                                //Associated cells group of starting cell
                Bit81 used = new Bit81();

                foreach( var noS in PStart.FreeB.IEGet_BtoNo() ){                   //Choose one digit(in PStart)
                    int noB = (1<<noS);  
                    Bit81 Bpat_noS = Bpat[noS]?? (new Bit81(pBOARD,noB));
                    Bit81 Bpat_sol = CnctdCs & Bpat_noS;                    //here if there is a solution
                    used.Clear();                                           //determine used

                    //--- prepare ---
                        //WriteLine($"selected noS: #{noS+1} Bpat_sol:{Bpat_sol}" );
                    int no0 = PStart.FreeB.BitReset(noS).BitToNum();                //The other digit of the starting cell
                    ChainXY P0 = new ChainXY(no0,PStart,null);
                    Que.Enqueue( P0 );
                    ChainXYLst.Add( P0 );
                    used.BPSet(P0.UCe.rc);
                    //WriteLine($"start p0:{P0}");

                    //--- search ---    
                    while(Que.Count>0){                                             //Extend the chain step by step
                        var P1 = Que.Dequeue();

                        foreach( var LKnext in CeLKMan.IEGetRcNoType(P1.UCe.rc,P1.no,1) ){  //strongLink connected with P1, #no
                            UCell UCe2 = LKnext.UCe2;
                            if( !BP_bivalue.IsHit(UCe2.rc) )  continue;
                            if( used.IsHit(UCe2.rc) )  continue;
                            used.BPSet(UCe2.rc);

                            int no2 = (UCe2.FreeB.BitReset(P1.no)).BitToNum();      // no2:other digit
                            ChainXY P2 = new ChainXY(no2,UCe2,P1);                  // UCe2:next cell, P1:upstream cell
         
                            Que.Enqueue( P2 );
                            ChainXYLst.Add(P2);

                            if( no2==noS ){
                                foreach( var rc in Bpat_sol.IEGetRC()){             // here if there is a solution
                                    if( ConnectedCells[rc].IsHit(UCe2.rc) ){
                                        //--- found ---
                                        List<ChainXY> SolChain = new List<ChainXY>();
                                        SolChain.Add(P2);
                                        ChainXY PX = P2.pre;
                                        while(PX!=null){ SolChain.Add(PX); PX=PX.pre; } // follow the chain upstream.
                                        SolChain.Reverse();
                                        yield return ( pBOARD[rc], noS, SolChain );
                                        goto LBreak;
                                    }
                                }
                            }
                        }
                    }
                  LBreak:
                    continue;
                        //WriteLine("---------");
                }
            }
            yield break;
        }

        private class ChainXY{
            public int   no;
            public UCell UCe;
            public ChainXY pre;

            public ChainXY( int no, UCell UCe, ChainXY pre){
                this.no  = no;
                this.UCe = UCe;
                this.pre  = pre;
            }

            public override string ToString(){
                string  rcX = (pre==null)? "-": (pre.UCe.rc).ToRCString();
                string st = $"sel:#{no+1} {UCe.ToStringN2()} pre:{rcX}";
                return st;
            }

        }
    }
}