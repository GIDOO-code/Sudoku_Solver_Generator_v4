 using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{
		private int stageNoMemo = -9;
		private List<UCell> BVCellLst;

        public NXGCellLinkGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }
		private void Prepare(){
			if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				CeLKMan.Initialize();
				BVCellLst=null;
			}      
		}

        //Skyscraper is an algorithm consisting of two StrongLinks.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page40.html
     
        public bool Skyscraper(){
			Prepare();
			CeLKMan.PrepareCellLink(1);                                 //Generate StrongLink

            for(int no=0; no<9; no++){              
                var SSLst = CeLKMan.IEGetNoType(no,1).ToList();         //Select only StrongLink of #no
                if(SSLst.Count<=2) continue;                            

                // Care must be taken in configuring links.
                // Opposite links are also included in the list.
                // This simplifies the algorithm.
                var prm=new Combination(SSLst.Count,2);                 // Linked(SSLst) list contains both x-y and y-x
                int nxtX=1;
                while(prm.Successor(nxtX)){                
                    UCellLink UCLa = SSLst[ prm.Index[0] ];
                    UCellLink UCLb = SSLst[ prm.Index[1] ];
                 
                    if( (UCLa.B81|UCLb.B81).Count != 4 )  continue;     //All cells are different?

                    Bit81 ConA1 = ConnectedCells[UCLa.rc1];             //ConA1:cell group related to cell rc1
                    Bit81 ConA2 = ConnectedCells[UCLa.rc2];             //ConA2:cell group related to cell rc2

                    // There are two strong links. Of the four edge cells, only one set is connected.
                    if( !ConA1.IsHit(UCLb.rc1) )  continue;             //<UCLa.rc1> and <UCLb.rc1> are connected?

                    if(  ConA1.IsHit(UCLb.rc2) )  continue;             //<UCLa.rc1> and <UCLb.rc2> are not connected? 
                    if(  ConA2.IsHit(UCLb.rc1) )  continue;             //<UCLa.rc2> and <UCLb.rc1> are not connected?  
                    if(  ConA2.IsHit(UCLb.rc2) )  continue;             //<UCLa.rc2> and <UCLb.rc2> are not connected?  

                    //Only UCLa.rc1 and UCLb.rc1 belong to the same house.

                    Bit81 ELM = ConA2 & ConnectedCells[UCLb.rc2];
                    ELM -= (ConA1 | ConnectedCells[UCLb.rc1]);          //ELM:eliminatable cells

                    bool SSfound = false;
                    int noB = (1<<no); 
                    foreach(UCell P in ELM.IEGetUCell_noB(pBOARD,noB)){ P.CancelB=P.FreeB&noB; SSfound=true; }
                    if(!SSfound)  continue;     //Skyscraper found

                #region Result
                    SolCode =2;                
                    if( SolInfoB ){
                        pBOARD[UCLa.rc1].Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);
                        pBOARD[UCLa.rc2].Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);
                        pBOARD[UCLb.rc1].Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);
                        pBOARD[UCLb.rc2].Set_CellColorBkgColor_noBit(noB,AttCr,SolBkCr);

                        string msg="\r", msg2="";
                        msg += $"  on {(no+1)} in {UCLa.rc1.ToRCNCLString()} {UCLb.rc1.ToRCNCLString()}";
                        msg += $"\r  connected by {UCLa.rc2.ToRCNCLString()} {UCLb.rc2.ToRCNCLString()}";
                        msg += "\r  eliminated ";
                        foreach(UCell P in ELM.IEGetUCell_noB(pBOARD,noB)){ msg2 += " "+P.rc.ToRCString(); }
                        msg2 = " "+msg2.ToString_SameHouseComp();
                        ResultLong = "Skyscraper" + msg+msg2;
                        Result = $"Skyscraper #{(no+1)} in {msg2}";
                       // WriteLine( $"Skyscraper #{(no+1)} in {msg2}" );  //*********
                    }
                    else Result = $"Skyscraper #{(no+1)}";
                #endregion Result
                    if( __SimpleAnalyzerB__ )  return true;
                    if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                }
            }
            return false;
        }
    }
}