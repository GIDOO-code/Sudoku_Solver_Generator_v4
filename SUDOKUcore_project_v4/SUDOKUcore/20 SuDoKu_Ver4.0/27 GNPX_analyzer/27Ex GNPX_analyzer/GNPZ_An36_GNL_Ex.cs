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
    public partial class GroupedLinkGen: AnalyzerBaseV2{
		public static DevelopWin devWin; //## development

        private const int     S=1, W=2;
		private int  stageNoMemo = -9;
        public  int  NiceLoopMax{ get => (int)GNPX_App.GMthdOption["NiceLoopMax"]; }
        private int  SolLimBrk=0;
        private int  __SolGL=-1;

        // *=*=* Updated to radiation search *=*=*


        // GroupedNiceLoopEx
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page55.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //47....3.99...4728...8.9...7...81..4..167.48...8..6..2.85..7...4.6.4.5..81.3...57.
        //1526.7.893...5...4...9.3..75.8...2.6.6.....9.9.3...4.14..5.6...6...3...573.4.1962

        public bool GroupedNiceLoopEx( ){
			Prepare();
            pSprLKsMan.PrepareSuperLinkMan( AllF:true );

            //***************************************************
            bool DevelopB=false;        // true : on development
            //***************************************************

			foreach( var P0 in pBOARD.Where(p=>(p.FreeB>0)) ){                        // Stem Cell

				foreach( var noH in P0.FreeB.IEGet_BtoNo() ){                       // Stem Digit
					int noB=(1<<noH);

                    foreach( var GLKH in pSprLKsMan.IEGet_SuperLinkFirst(P0,noH)){  // First Link (3=S/W any)
                        if( pAnMan.Check_TimeLimit() ) return false;

                        //=============================================================
                        UCell P000=GLKH.UGCellsA[0];    //P000=P0

                        //Find the chain of links starting at GLKH and reaching P000(cell).
					    USuperLink GNL_Result = pSprLKsMan.GNL_EvalSuperLinkChain( GLKH, P000.rc, DevelopB:DevelopB );
                        //=============================================================

                        if(GNL_Result!=null){       //***** Solved   
                            string st3="";
                            string st = _chainToStringGNL( GNL_Result, ref st3 );
                            if(DevelopB)  WriteLine($"***** solved:{st}");
                                                               
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                        }
                    }
				}
            }
            return false;
        }

        public  string _chainToStringGNL( USuperLink GNL_Result, ref string st3 ){
            string st="";
            if( GNL_Result==null )  return st;

            GroupedLink GLKnxt = GNL_Result.resultGLK;
            //pSprLKsMan.Debug_ChainPrint(GLKnxt);
            var SolLst = pSprLKsMan.Convert_ChainToList_GNL(GNL_Result);
            GroupedLink GLKorg=SolLst[0];

            {//===================== cells coloring ===========================
                foreach( var LK in SolLst ){
                    bool bALK = LK is ALSLink;
                    int type = (LK is ALSLink)? S: LK.type;//ALSLink, in ALS, is S
                    foreach( var P1 in LK.UGCellsA.Select(p=>pBOARD[p.rc])){
                        //WriteLine($"---------- {P1}");
                        int noB=(1<<LK.no);
                        if(!bALK)    P1.Set_CellBKGColor(SolBkCr);
                        if(type==S){ P1.Set_CellDigitsColor_noBit(noB,AttCr2);  }
                        else{        P1.Set_CellDigitsColor_noBit(noB,AttCr3); }
                    }

                    if(type==W){
                        foreach( var P2 in LK.UGCellsB.Select(p=>pBOARD[p.rc])){
                            int noB2=(1<<LK.no);
                            if(!bALK)  P2.Set_CellBKGColor(SolBkCr);
                            P2.Set_CellDigitsColor_noBit(noB2,AttCr);
                        }
                    }
                }

                int cx=2;
                foreach( var LK in SolLst ){    // ALS
                    ALSLink ALK = LK as ALSLink;
                    if(ALK==null)  continue;
                    Color crG=_ColorsLst[cx++];
                    foreach( var P in ALK.ALSbase.B81.IEGet_rc().Select(rc=>pBOARD[rc]) ){
                        P.Set_CellBKGColor(crG);
                    }
                }
            }

            {//===================== result report ===========================
                st3="";
                int SolType = GNL_Result.contDiscontF;
                if(SolType==1) st = "Nice Loop+(Continuous)";  //<>continuous
                else{                                              //<>discontinuous
                    int rc=GLKorg.UGCellsA[0].rc;
                    var P=pBOARD[rc];
                    st = $"Nice Loop+(Discontinuous) {rc.ToRCString()}";
                    int dcTyp = GLKorg.type*10 + GLKnxt.type; 
                    switch(dcTyp){
                        case 11: P.FixedNo = GLKorg.no+1; break;
                        case 12: P.CancelB = 1<<GLKnxt.no; break;
                        case 21: P.CancelB = 1<<GLKorg.no; break;
                        case 22: P.CancelB = 1<<GLKorg.no; break;
                    }
                }

                string st2 = __chainToStringGNLsub( SolLst, ref st3 );
                st = st3+st;
                Result = st;
                ResultLong = st +"\r"+st2 + "\r\r"+_sol_Truth_Message();
            }
            return st;
        }

        private string _sol_Truth_Message(){
            string  stT="", stF="";
            
            var no_rcLst = new string[9];
            foreach( var P in pBOARD.Where(p=>p.FixedNo>0) ){
                int noT = P.FixedNo-1;
                if( no_rcLst[noT] is null ) no_rcLst[noT] = "";
                no_rcLst[noT] += " " + P.rc.ToRCString();;
            }
            for( int no=0; no<9; no++ ){
                if( no_rcLst[no] is null )  continue;
                stT += " " + no_rcLst[no].ToString_SameHouseComp( $"#{no+1}" );
            }
            if( stT != "" ) stT = stT + " is true";

            no_rcLst = new string[9];
            foreach( var P in pBOARD.Where(p=>p.CancelB>0) ){
                int canB = P.CancelB;
                string rcSt = " " + P.rc.ToRCString();
                foreach( var no in canB.IEGet_BtoNo() ){
                    if (no_rcLst[no] is null ) no_rcLst[no] = "";
                    no_rcLst[no] += rcSt;
                }
            }
            for( int no=0; no<9; no++ ){
                if( no_rcLst[no] is null )  continue;
                stF += " " + no_rcLst[no].ToString_SameHouseComp( $"#{no+1}" );
            }
            stF = stF.ToString_SameHouseNoComp( );
            if( stF != "" ) stF = stF + " is false";
            
            string st = stT;          
            if( stT=="" && stF!="" ) st = stF;
            if( stT!="" && stF!="" ) st += "\r" + stF;
        
            return st;
        }

        public  string __chainToStringGNLsub(List<GroupedLink> SolLst, ref string st3){
            string st = $"[{SolLst[0].UGCellsA}]";
            foreach( var LK in SolLst ){
                string ST_LinkNo="";
                ALSLink ALK=LK as ALSLink;
                if(ALK!=null){
                    ST_LinkNo = $"-#{(ALK.no+1)}ALS<{ALK.ALSbase.ToStringRC()}>#{(ALK.no2+1)}-";
                }
                else{
                    string mk = (LK.type==1)? "=": "-";
                    ST_LinkNo = mk+(LK.no2+1)+mk;
                }
                st += $"{ST_LinkNo}[{LK.UGCellsB}]";
            }
            
            if(st.Contains("ALS") || st.Contains("[<")) st3="Grouped ";
            return st;
        }
    }  
}