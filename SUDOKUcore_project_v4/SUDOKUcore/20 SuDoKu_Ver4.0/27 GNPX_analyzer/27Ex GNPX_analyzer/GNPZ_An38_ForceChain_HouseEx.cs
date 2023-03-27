using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Media;
using static System.Diagnostics.Debug;
using System.Threading;

using GIDOO_space;

namespace GNPXcore{


	// __SolGL   <- —p“r@check #########################################

    public partial class GroupedLinkGen: AnalyzerBaseV2{
		public bool ForceChain_HouseEx( ){
            if( !ForceChain_on ) return false;    
            
            GroupedLink._IDsetB=false;  //ID set for debug

			Prepare();
            pSprLKsMan.PrepareSuperLinkMan( AllF:true );
              
            Bit81[] multiPathB81=new Bit81[9];
            for(int no=0; no<9; no++ ) multiPathB81[no]=new Bit81();
				
			extStLst.Clear();

			// ========== Select House ==========
			for(int hs0=0; hs0<27; hs0++ ){ // house:[ 0-8:row 9-17:column 18-26:block ] 
				int noBs = pBOARD.IEGetCellInHouse(hs0).Aggregate(0,(Q,P)=>Q|(P.FreeB));

				// ========== Select Digit ==========
				foreach( var no0 in noBs.IEGet_BtoNo() ){
					int noB=(1<<no0);                
					Bit81[] sTrue=new Bit81[9];
					for( int no=0; no<9; no++ ) sTrue[no]=new Bit81(all1:true);

					// ---------- For Cell P0 in House hs0 ----------
					foreach( var PX in pBOARD.IEGetCellInHouse(hs0,noB) ){
		                if(pAnMan.Check_TimeLimit()) return false;

						USuperLink USLK = pSprLKsMan.get_L2SprLK( PX.rc, no0, FullSearchB:false, DevelopB:false);
						if( USLK==null || !USLK.SolFound )  continue;

                        for( int no=0; no<9; no++ ){
                            sTrue[no] &= (USLK.Qtrue[no] - USLK.Qfalse[no]);
                            sTrue[no].BPReset(PX.rc);
                        }
					}

					bool solvedSingle=false;                    		
                    for( int noX=0; noX<9; noX++ ){
					    if( sTrue[noX].IsNotZero() )  solvedSingle=true;
					}

					// ---------- Solution found ----------
					if( solvedSingle ){
						foreach( var _ in _ForceChainHouseDispEx( multiPathB81, sTrue, hs0, no0 ) ){ 
							if( ForceChain_Option == "ForceL1" ){
								if( __SimpleAnalyzerB__ )  return (SolCode>0);
								if( !pAnMan.SnapSaveGP(pPZL) ) return (SolCode>0);
								extResult = ""; extStLst.Clear();  
							}
						}
					}
				}

				if( SolInfoB && ForceChain_Option=="ForceL2" && extResult!="" ){
					if( __SimpleAnalyzerB__ )  return (SolCode>0);
					if( !pAnMan.SnapSaveGP(pPZL) ) return (SolCode>0);
					extResult = ""; extStLst.Clear();
				}
            }

            if( SolInfoB && ForceChain_Option=="ForceL3" && extResult!="" ){
                Result = ResultLong = "ForceChain_House";
                if( __SimpleAnalyzerB__ )  return (SolCode>0);
				if( !pAnMan.SnapSaveGP(pPZL) ) return (SolCode>0);
            }
            return (SolCode>0);
        }



		private  IEnumerable<bool> _ForceChainHouseDispEx( Bit81[] multiPathB81, Bit81[] sTrue, int hs0, int no0 ){
			string st0="", st2="";     
					
			List<string>  extStLstTmp = new List<string>();

			for( int noX=0; noX<9; noX++ ){	
				if( sTrue[noX].IsZero() )  continue;
				foreach( var rc in sTrue[noX].IEGet_rc() ){
                    if( !showPrfMltPathsB && multiPathB81[noX].IsHit(rc) ) continue;        // omitted when there are multiple proofs
					multiPathB81[noX].BPSet(rc);

					UCell PX = pBOARD[rc];
					PX.FixedNo = noX+1;
					int elm = PX.FreeB.DifSet(1<<noX);
					PX.CancelB = elm;			
					SolCode=1;

					if( SolInfoB ){
						PX.Set_CellColorBkgColor_noBit( 1<<noX, Colors.Red , Colors.LightGreen );
						PX.Set_CellDigitsColorRev_noBit( elm, Colors.Red );

						st0 = $"ForceChain_House({_HouseToString(hs0)}#{(no0+1)}) {PX.rc.ToRCString()}#{(noX+1)} is true";
						string st1="";
						foreach( var P in pBOARD.IEGetCellInHouse(hs0,1<<no0) ){
							USuperLink USLK = pSprLKsMan.get_L2SprLK( P.rc, no0, FullSearchB:true, DevelopB:false ); //Accurate path
							st1 += "\r"+pSprLKsMan._GenMessage2true( USLK, PX, noX );
							if( ForceChain_Option!="ForceL3" ) P.Set_CellColorBkgColor_noBit( 1<<no0, Colors.Green , Colors.Yellow );
						}

						st2 = (st0+st1).Trim();
						extResult = st2;                             //(Description of each solution)
						if( ForceChain_Option!="ForceL1" ) extStLstTmp.Add(st2);
						Result = ResultLong = st0;
					}
					if( extStLstTmp.Count>0 ){
						extStLstTmp.Sort();
						var Q = extStLstTmp.Last();
						extStLstTmp.Remove(Q);
						extStLstTmp.Add( Q+"\r" );
						extStLst.AddRange(extStLstTmp);
						extResult = string.Join("\r",extStLst);   // (Description of each solution)
						extStLstTmp.Clear();
					}
					yield return (SolCode>0);

				}
			}
    
			yield break;
			
        }
		

        private string _HouseToString(int hs){
			string  st;
			if(hs<9)  st = $"row{(hs+1)}";
			else if(hs<18) st = $"col{(hs-8)}";
			else st = "blk"+(hs-17);
			return  st;
		}
    }  
}
