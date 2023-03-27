using System;
using System.Collections.Generic;
using System.Linq;

using GIDOO_space;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{

        //W-Wing is an algorithm composed of bivalue cell and link.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page43.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //..973..81.8...9...7.5.84..33....82.74.2.......786..4.5...8.6..26........8.74.15.6
        //.4......512..7..8667..9.32......7..2..28.37..4..2......93.8..5771..5..638......9.
        public bool  Wwing( ){ 
			Prepare(); 
            CeLKMan.PrepareCellLink(1);    //Generate StrongLink

            if(BVCellLst==null)  BVCellLst = pBOARD.FindAll(p=>(p.FreeBC==2));//BV:BiValue
            if(BVCellLst.Count<2) return false;    
            BVCellLst.Sort((A,B)=>(A.FreeB-B.FreeB));                       //Important!!!

            bool Wwing=false;
            var  cmb = new Combination(BVCellLst.Count,2);
            int nxt=99;
            while(cmb.Successor(skip:nxt)){
                UCell P = BVCellLst[cmb.Index[0]];
                UCell Q = BVCellLst[cmb.Index[1]];
                nxt=1;
                if(P.FreeB!=Q.FreeB){ nxt=0; continue; }                    //BVCellLst is sorted, so possible to skip.            
                if(ConnectedCells[P.rc].IsHit(Q.rc)) continue;

                foreach( var LK in CeLKMan.IEGetCellInHouse(typB:1) ){      //1:StrongLink(Link has a direction. The opposite link is different.)
                    int no1B = (1<<LK.no);
                    if( (P.FreeB&no1B)==0 ) continue;
                    if( LK.rc1==P.rc || LK.rc2==Q.rc ) continue;
                    if( !ConnectedCells[P.rc].IsHit(LK.rc1) )  continue;    //LK.rc1 is in the connected area of ​​P.rc?
                    if( !ConnectedCells[Q.rc].IsHit(LK.rc2) )  continue;    //LK.rc2 is in the connected area of Q.rc?
                    int no2B = P.FreeB.BitReset(LK.no);                     //another digit
                    
                    string msg2="";
                    Bit81 ELM = ConnectedCells[P.rc] & ConnectedCells[Q.rc];//ELM:Common part of the influence area of ​​cell P and cell Q
                    foreach( var E in ELM.IEGetUCell_noB(pBOARD,no2B) ){         //Cell/digit in ELM can be excluded
                        E.CancelB = no2B; Wwing=true;                       //W-Wing found
                        if( SolInfoB ) msg2 += " "+E.rc.ToRCString();
                    }
                    if( !Wwing ) continue;

                    // === found ===
                    SolCode=2;
                    if( SolInfoB ){
                        UCell A=pBOARD[LK.rc1], B=pBOARD[LK.rc2];
                        int noBX=P.FreeB.DifSet(no2B);
                        P.Set_CellColorBkgColor_noBit( noBX, AttCr, SolBkCr2 );
                        Q.Set_CellColorBkgColor_noBit( noBX, AttCr, SolBkCr2 );
                     
                        A.Set_CellColorBkgColor_noBit( no1B, AttCr, SolBkCr );
                        B.Set_CellColorBkgColor_noBit( no1B, AttCr, SolBkCr );

                        string msg0 = $" bvCell: {_XYwingResSub(P)} ,{_XYwingResSub(Q)}";
                        string msg1 = $"  SLink: {A.rc.ToRCNCLString()}-{B.rc.ToRCNCLString()}(#{(LK.no+1)})";
                        Result = $"W Wing Eli.;#{(no2B.BitToNum()+1)} in {msg2.ToString_SameHouseComp()}";
                        ResultLong = "W Wing\r"+msg0+"\r"+msg1
                                   + $"\r Eliminated: #{(no2B.BitToNum()+1)} in {msg2.ToString_SameHouseComp()}";
                    }

                    if( __SimpleAnalyzerB__ )  return true;
                    if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                    Wwing=false;
                }
            }
            return false;
        }
    }
}