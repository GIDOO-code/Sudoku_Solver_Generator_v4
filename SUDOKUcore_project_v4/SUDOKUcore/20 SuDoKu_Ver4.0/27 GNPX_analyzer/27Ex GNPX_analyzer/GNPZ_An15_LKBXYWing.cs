using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

using GIDOO_space;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{

        //XYwing is an algorithm that consists of two WeakLinks with common cells.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page42.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //.1..7.69.4.6.9..1.5.9.2...87....9....9..3..8....8....41...6.8.9.8..4.7.5.67.8..4.
        //6..1....7..18.73...2.3...9..5.9.8641.........1482.6.3..7...2.1...64.18..8....3..4
        //..8..5..6.3.......6.2.4.5.7...384.59..65..2.39..7...4...4.5.8.....2.8...8.946....
        public bool XYwing( ){
			Prepare(); 
            CeLKMan.PrepareCellLink(2);                                         //Generate WeakLinks

            if(BVCellLst==null)  BVCellLst = pBOARD.FindAll(p=>(p.FreeBC==2));    //Generate BVs(BV:bivalue).
            if(BVCellLst.Count<3) return false;     

            bool XYwing=false;
            foreach( var UCeStart in BVCellLst ){                               //Choose one BV_Cell(=>PS)
                List<UCellLink> BVLKLst =CeLKMan.IEGetRcNoBTypB(UCeStart.rc,0x1FF,2).Where(L=>L.BVFlag).ToList();
                                                                                //Extract WeakLinks starting from UCeStart
                    //foreach( var P in BVLKLst ) WriteLine(P);
                if(BVLKLst.Count<2) continue;

                var cmb = new Combination(BVLKLst.Count,2);
                int nxt=1;
                while(cmb.Successor(skip:nxt)){                                 //Combine two WeakLinks from BVLKLst
                    UCellLink LKA=BVLKLst[cmb.Index[0]], LKB=BVLKLst[cmb.Index[1]];  //two BV_links connecting to UCeStart
                    UCell UCeA2=LKA.UCe2, UCeB2=LKB.UCe2;                       //other cells in BV_link  
                    if( UCeA2.rc==UCeB2.rc || LKA.no==LKB.no ) continue;        //two BV_links have different end and different digits

                    Bit81 Q81 = ConnectedCells[LKA.rc2]&ConnectedCells[LKB.rc2];
                    if( Q81.Count<=0 ) continue;                                //two W_links have cells connected indirectly

                    int noB = UCeA2.FreeB.DifSet(1<<LKA.no) & UCeB2.FreeB.DifSet(1<<LKB.no);
                    if( noB==0 ) continue;                                      //two W_links have common digit(=>no).
                    int no = noB.BitToNum();

                    string msg2="";
                    foreach( var A in Q81.IEGetUCell_noB(pBOARD,noB) ){
                        if( A==UCeStart || A==UCeA2 || A==UCeB2 ) continue;
                        A.CancelB=noB; XYwing=true;                             //cell(A)/digit(no) can be excluded
                        if( SolInfoB ) msg2+= $" {A.rc.ToRCNCLString()}(#{no+1})";
                    }

                    // === found ===
                    if(XYwing){
                        SolCode=2;
                        Color Cr = _ColorsLst[0];
                        UCeStart.Set_CellDigitsColor_noBit( UCeStart.FreeB, AttCr ); 
                        UCeStart.Set_CellBKGColor(SolBkCr);
                        UCeA2.Set_CellBKGColor(Cr); 
                        UCeB2.Set_CellBKGColor(Cr);

                        string msg0= $" Pivot: {_XYwingResSub(UCeStart)}";
                        string msg1= $" Pin: {_XYwingResSub(UCeB2)} ,{_XYwingResSub(UCeA2)}";
                        Result="XY Wing"+msg0;
                        if( SolInfoB ) ResultLong=$"XY Wing\r     {msg0}\r       {msg1}\r Eliminated:{msg2}";

                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        XYwing=false;
                    }
                }
            }
            return false;
        }
        private string _XYwingResSub( UCell P ){
            string st=P.rc.ToRCNCLString()+"(#"+P.FreeB.ToBitStringNZ(9)+")";
            return st;
        }
    }
}