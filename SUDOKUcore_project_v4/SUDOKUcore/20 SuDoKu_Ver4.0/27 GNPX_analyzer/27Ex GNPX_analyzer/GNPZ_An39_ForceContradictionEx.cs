using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Collections.Immutable;
using System.Windows.Ink;
using System.Collections;

// "pSprLKsMan.get_L2SprLKEx":
// Specify cells and digits.
// If this is true, find the cells and digits that prove true in the link concatenation.
// Similarly, find cells/digits that are proved to be false.
// The links use inter-cell links, ALS-links and AIC-links.

namespace GNPXcore{
    public partial class GroupedLinkGen: AnalyzerBaseV2{

        // ForceChain_ContradictionEx
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page56.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //1526.7.893...5...4...9.3..75.8...2.6.6.....9.9.3...4.14..5.6...6...3...573.4.1962

        public bool ForceChain_ContradictionEx( ){

           if( !ForceChain_on ) return false;   

            GroupedLink._IDsetB=false;  //ID set for debug
			Prepare();

			List<int> multiPath_TP = new List<int>();
            pSprLKsMan.PrepareSuperLinkMan( AllF:true );
			Bit81[] GLC = new Bit81[9];
			for(int k=0; k<9; k++ ) GLC[k]=new Bit81();  

			extResult = "";
			Result = ResultLong = "";
			foreach( var P0 in pBOARD.Where(p=>p.No==0) ){
                if(pAnMan.Check_TimeLimit()) return false;

				foreach( var no in P0.FreeB.IEGet_BtoNo() ){
					if(pAnMan.Check_TimeLimit()) return false;
					if( !showPrfMltPathsB ){
						int TP2 = (P0.rc<<16) | no;
						if( multiPath_TP.Contains(TP2) ) continue;	// omitted when there are multiple proofs
						multiPath_TP.Add(TP2);
					}

                    USuperLink USLK = pSprLKsMan.get_L2SprLKEx( P0.rc, no, FullSearchB:false, DevelopB:false );
					if( USLK != null ){
                        Bit81 sContradict = new Bit81();

                        for( int no2=0; no2<9; no2++ ){
                            sContradict = USLK.Qtrue[no2] & USLK.Qfalse[no2];
                            if( sContradict.IsZero() ) continue;

							foreach( var _ in ForceChain_ContradictionExDisp( sContradict, P0, no, USLK, no2, GLC ) ){
								if( pAnMan.Check_TimeLimit() ) return false;

								if( ForceChain_Option=="ForceL1" ){  
                                    if( __SimpleAnalyzerB__ )		return (SolCode>0);
								    if( !pAnMan.SnapSaveGP(pPZL) )  return (SolCode>0);
								    extResult = "";
								    Result = ResultLong = "";
							    }
								if( !showPrfMltPathsB )  goto L_showPrfMltPathsB_break;
							}
							
						}
					}
				}

			L_showPrfMltPathsB_break:

				if( SolInfoB && ForceChain_Option=="ForceL2" && extResult!="" ){
					if( __SimpleAnalyzerB__ )  return (SolCode>0);
					if( !pAnMan.SnapSaveGP(pPZL) ) return (SolCode>0);
					extResult = "";
					Result = ResultLong = "";
				}

            }

			if( SolInfoB && ForceChain_Option=="ForceL3" ){	
				Result = ResultLong = "ForceChain_Contradiction";
                if( __SimpleAnalyzerB__ )  return (SolCode>0);;
				if( !pAnMan.SnapSaveGP(pPZL) ) return (SolCode>0);;
			}

            return (SolCode>0);
        }


        private  IEnumerable<bool> ForceChain_ContradictionExDisp( Bit81 sContradict, UCell P0, int no, USuperLink USLK, int no2, Bit81[] GLC ){
			int noB = 1<<no;

			foreach( var PX in sContradict.IEGet_rc().Select(rc=>pBOARD[rc]) ){

				P0.CancelB |= noB;
				int E = P0.FreeB.DifSet(P0.CancelB);
				SolCode = (E.BitCount()==1)? 1: 2;
				string st0 = $"ForceChain_Contradiction {P0.rc.ToRCString()}#{(no+1)} is false";
				Result = ResultLong = st0;

				// ---------- Solved ----------
					if( SolInfoB ){
						P0.Set_CellBKGColor( Colors.LightGreen );	
						if( ForceChain_Option=="ForceL1" ){
							PX.Set_CellColorBkgColor_noBit( PX.FreeB, Colors.Green, Colors.Yellow );
						}

						P0.Set_CellDigitsColorRev_noBit(noB,Colors.Red );
						if( E.BitCount()==1)  P0.Set_CellDigitsColor_noBit( E, Colors.Red );												
						GLC[no].BPSet(P0.rc);
                          
						string st1 = pSprLKsMan._GenMessage2true( USLK, PX, no2 );
						st1 += "\r"+ pSprLKsMan._GenMessage2false( USLK, PX, no2);
						string st2 = ($"{st0}\r{st1}").Trim();
						extResult += (st2+"\r");
					}
					yield return true ;

            }
			yield break;
		}

        private  IEnumerable<bool> _developDisp2Ex( Bit81[] GLC ){
			List<UCell> qBDL = new List<UCell>();
			List<string>  extStLstTmp = new List<string>();

            pBOARD.ForEach( p => qBDL.Add(p.Copy()) );
			foreach( var PX in qBDL.Where( q => q.FreeB>0 ) ){
				int E=0;
				for(int n=0; n<9; n++ ){ if( GLC[n].IsHit(PX.rc) ) E|=(1<<n); }
				if( E > 0 ){
					UCell P=pBOARD[PX.rc];
					P.CancelB = E;   //SolInfoB:Flag whether to generate solution information
					if( SolInfoB ){
						P.Set_CellColorBkgColor_noBit(  E, Colors.White , Colors.PowderBlue );
						P.Set_CellDigitsColorRev_noBit( E, Colors.Blue );

						P.Set_CellColorBkgColor_noBit(  E, Colors.White , Colors.PowderBlue );
						P.Set_CellDigitsColorRev_noBit( E, Colors.Red );
						int sb = P.FreeB.DifSet(E);
						if( sb.BitCount()==1 ){
							P.FixedNo=sb.BitToNum()+1;
							SolCode=1;
						}
						else if(sb.BitCount()==0){ P.Set_CellBKGColor( Colors.Violet ); }
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
				yield break;
            }
            devWin.Set_dev_GBoard( qBDL );
        }
    }
}