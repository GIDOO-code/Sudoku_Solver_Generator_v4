using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using static System.Diagnostics.Debug;
using System.Threading;

using GIDOO_space;
using System.Windows.Ink;
using System.Collections;

namespace GNPXcore{
    public partial class GroupedLinkGen: AnalyzerBaseV2{

/*
    Force-based algorithms use the current links.
    The chain is assembled using this links, and the truth or false of the cell candidate is logically derived.

    The Force algorithm is based on the following logic.

     (1) Set X has one element true and the rest false. Which is true is undecided.
     (2) In a chain starting with true element, the value of the derived element is determined to be true or false.
     (3) In a chain starting with false element, the value of the derived element is uncertain (it can be true or false).
     (4) For each chain that starts assuming each element of set Ⅹ as true, 
         the authenticity of element A is determined when the true/false values ​​of element A leading by all chains match.
     (5) In the chain that starts assuming that one element B of set X is true, 
         when the true/false values ​​of the element C guided by multiple routes do not match,
         the starting element B is determined to be false. 

     https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/
     https://github.com/GIDOO-code/Sudoku_Solver_Generator/tree/master/docs/page56.html
 */ 

        // "pSprLKsMan.get_L2SprLKEx":
        // Specify cells and digits.
        // If this is true, find the cells and digits that prove true in the link concatenation.
        // Similarly, find cells/digits that are proved to be false.
        // The links use inter-cell links, ALS-links and AIC-links.

        // ForceChain_CellEx
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page56.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //1526.7.893...5...4...9.3..75.8...2.6.6.....9.9.3...4.14..5.6...6...3...573.4.1962


		private	List<string>  extStLst = new List<string>();
        public bool ForceChain_CellEx( ){
            if( !ForceChain_on )  return false;

            // ========== Prepare ==========
            GroupedLink._IDsetB=false;  //ID set for debug
			Prepare();

            pSprLKsMan.PrepareSuperLinkMan( AllF:true );
              
			extStLst.Clear();
            // ========== Search ==========
        	
            // ========== Select Stem Cell ==========
			foreach( var P0 in pBOARD.Where(p=>(p.FreeB>0)) ){
			    Bit81[] sTrue=new Bit81[9];
			    for(int k=0; k<9; k++ ) sTrue[k] = new Bit81(all1:true);    // Set all cells for all digits to true(set all bits to 1).

                bool  solvedSingle = false;
                //========== Test with P0 and no0 ==========
				foreach( var no0 in P0.FreeB.IEGet_BtoNo() ){
					int noB=(1<<no0);
					USuperLink USLK = pSprLKsMan.get_L2SprLKEx( P0.rc, no0, FullSearchB:true, DevelopB:false );
					if( USLK==null || !USLK.SolFound )  goto nextSearch;              
                    for(int k=0; k<9; k++ ){
                        sTrue[k] &= (USLK.Qtrue[k] - USLK.Qfalse[k]);       // And-accumulate cells/digits that can only be proven "true".
                    }                  
                }

				for(int no=0; no<9; no++){
					sTrue[no].BPReset(P0.rc);                              // Exclude stem cells/digits
					if( sTrue[no].IsNotZero() )  solvedSingle = true;      // Cells/digit that can be proved are solutions.
				}

                
				// ---------- Solution found ----------
                if( solvedSingle ){
                    for(int no0=0; no0<9; no0++){
                        if( sTrue[no0].IsNotZero() )  continue;
                        foreach( var _ in _ForceChainCellDispEx( sTrue, P0.rc, no0 ) ){
                            if( ForceChain_Option == "ForceL1" ){
                                if( __SimpleAnalyzerB__ )  return true;
				                if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                                extResult = ""; extStLst.Clear();
                            }
                        }
                    }
                    if( ForceChain_Option=="ForceL2" && extResult!="" ){
                        if( __SimpleAnalyzerB__ )  return true;
				        if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        extResult = ""; extStLst.Clear();
                    }
                }

			  nextSearch:
                continue;
            }
            if( SolInfoB && ForceChain_Option=="ForceL3" && extResult!="" ){    
                if( __SimpleAnalyzerB__ )  return true;
				if( !pAnMan.SnapSaveGP(pPZL) ) return true;
            }

            return (SolCode>0);
        }

        private  IEnumerable<bool> _ForceChainCellDispEx( Bit81[] sTrue, int rc0, int no0 ){
			UCell P0 = pBOARD[rc0];
			List<string>  extStLstTmp = new List<string>();

            for(int noX=0; noX<9; noX++ ){
				if( sTrue[noX].IsZero() )  continue;

				foreach( var rc in sTrue[noX].IEGet_rc() ){
                    if( pAnMan.Check_TimeLimit() )  yield break;

                    if( !showPrfMltPathsB && multiPathB81[noX].IsHit(rc) ) continue;        // omitted when there are multiple proofs
                    multiPathB81[noX].BPSet(rc);

					UCell PX = pBOARD[rc];
					PX.FixedNo = noX+1;                                                          // set to fixed(rc/no is true)
					int elm = PX.FreeB.DifSet(1<<noX);
					PX.CancelB = elm;                                                        // other digits are false
					SolCode = 1;                                                            // Fixed Flag  

                    // ========== display settings ==========
					if( SolInfoB ){   //SolInfoB:Flag whether to generate solution information
						P0.Set_CellColorBkgColor_noBit( P0.FreeB, Colors.Green, Colors.Yellow );

						PX.Set_CellColorBkgColor_noBit( 1<<noX, Colors.Red , Colors.LightGreen );//Fixed Digit
						PX.Set_CellDigitsColorRev_noBit( elm, Colors.Red );                      //False Digits
					
                        string st0 = $"ForceChain_Cell {PX.rc.ToRCString()}#{(noX+1)} is true";         // st0:Title of each solution
                        string st1="";
					    foreach( var no in pBOARD[rc0].FreeB.IEGet_BtoNo() ){
						    USuperLink USLK = pSprLKsMan.get_L2SprLKEx( rc0, no, FullSearchB:false, DevelopB:false ); // Get proof chain
						    st1 += "\r"+pSprLKsMan._GenMessage2true( USLK, PX, noX);         // st1:Exact GroupedLink path
					    }
                                                   // st2:Description of each solution
                        Result = ResultLong = st0; 
                        string st2 = (st0+st1).Trim();    
			            extStLstTmp.Add(st2);
                    }

                    if( extStLstTmp.Count>0 ){
                        extStLstTmp.Sort();
				        var Q = extStLstTmp.Last();
				        extStLstTmp.Remove(Q);
				        extStLstTmp.Add( Q+"\r" );
				        extStLst.AddRange(extStLstTmp);
                        extResult = string.Join("\r",extStLst);   // (Description of each solution)
                        extStLstTmp.Clear();
        
                        yield return (SolCode>0);
                    }

                }
			}
			yield break;
        }

        private string GenMessage2FakeProposition(int rc0, Bit81[] fakeP, USuperLink USLK, UCell Q ){   //q
            string st="";
            foreach( var no in pBOARD[rc0].FreeB.IEGet_BtoNo() ){
                if(fakeP[no].IsHit(rc0)){
                    st += $"ForceChain_Cell {rc0.ToRCString()}/{(no+1)} is false(contradition)";

                    st += "\r"+ pSprLKsMan._GenMessage2true(USLK,Q,no);
                    st += "\r"+ pSprLKsMan._GenMessage2false(USLK,Q,no);
                }
            }
            return st;
        }
    }  
}
