using System;
using System.Collections.Generic;
using System.Linq;
using GIDOO_space;
using static System.Diagnostics.Debug;

namespace GNPXcore{
    public partial class NiceLoopGen: AnalyzerBaseV2{
		private int stageNoMemo = -9;
        private const int S=1;
        public  int NiceLoopMax{ get => (int)GNPX_App.GMthdOption["NiceLoopMax"]; }

        public NiceLoopGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }

        private bool break_NiceLoop=false; //True if the number of solutions reaches the specified number.

        // NiceLoop
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page54.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //47....3.99...4728...8.9...7...81..4..167.48...8..6..2.85..7...4.6.4.5..81.3...57.
        //47....5.92...7361...3.9...7...56..9..613.47...5..2..6.52..3...6.3.1.2..51.9...24.
        //......3...385.1.4.5..37..6..76..389...9...7...157..42..9..15..4.8.6.753...2......
        //6.14..5.7.3.7..4..9..35.....6..1.3.415.....688.4.3..1.....94..2..2..3.4.4.6..21.3


		private void Prepare(){
			if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				CeLKMan.Initialize();
				CeLKMan.PrepareCellLink(1+2);
			}      
		}

        public bool NiceLoop( ){  //Depth-first Search
            break_NiceLoop=false;
			Prepare();

            CeLKMan.PrepareCellLink(1+2);    //Generate StrongLink(1), WeakLink(2)

            for( int szCtrl=4; szCtrl<NiceLoopMax; szCtrl++ ){                      // NL Size
                foreach( var P0 in pBOARD.Where(p=>(p.No==0)) ){                      // Stem Cell
                    foreach( var no in P0.FreeB.IEGet_BtoNo() ){                    // Stem Digit

                        foreach( var LKH in CeLKMan.IEGetRcNoType(P0.rc,no,3) ){    // First Link (3=S/W any)
                                                               // P0.rc:cell no:digit 3:S/W type

                            if( pAnMan.Check_TimeLimit() ) return false;   //Check_TimeLimit

                            {//----- Begin recursive search -----
                                var SolStack=new Stack<UCellLink>();
                                SolStack.Push(LKH);                 
                                Bit81 UsedCells=new Bit81(LKH.rc2);                 // Bit Representation of Used Cells

                                _NL_Search(LKH,LKH,SolStack,UsedCells,szCtrl-1);        
                                if( break_NiceLoop ) return true; //If suspended, immediately return to the parent routine.
                            }
                        }
                    }
                }
            }
            if( SolCode>0 ) return true;
            return false;
        }

        private bool _NL_Search( UCellLink LK0, UCellLink LKpre, Stack<UCellLink> SolStack, Bit81 UsedCells, int szCtrl ){
            if( szCtrl<=0 ) return false;

            foreach( var LKnxt in CeLKMan.IEGet_CeCeSeq(LKpre) ){
                // Links(LKnxt) connecting to LKpre that satisfy concatenation conditions

                int rc2Nxt = LKnxt.rc2;                             // next cell
                if( UsedCells.IsHit(rc2Nxt) ) continue;             // UsedCells do not contain Stem cell.

                { //===== Chain Search =====
                    SolStack.Push(LKnxt);  
                    //___Debug_Print_NLChain(SolStack);
                    if( rc2Nxt==LK0.rc1 && szCtrl==1 ){             // Loop was formed (the next cell matches the Stem Cell)
                        if( SolStack.Count>2 ){                     
                            int SolType = _NL_CheckSolution(LK0,LKnxt,SolStack,UsedCells);//Solved?
                            if( SolType>0 ){          
                                if( SolInfoB ) _NL_SolResult(LK0,LKnxt,SolStack,SolType);

                                if( __SimpleAnalyzerB__ ){ break_NiceLoop=true;  return true; }
                                if( !pAnMan.SnapSaveGP(pPZL) ){ break_NiceLoop=true; return true; }
                            }
                        }
                    }
                    else{
                        Bit81 UsedCellsNxt = UsedCells|(new Bit81(rc2Nxt));   // Create a new bit expression of used cell
                        _NL_Search(LK0,LKnxt,SolStack,UsedCellsNxt,szCtrl-1); // Next step Search(recursive call
                        if( break_NiceLoop )  return true;
                    }

                    SolStack.Pop();                                 //Failure(Cancel link extension processing）
                } //-----------------------------
            }  
            return false;
        }

        private int _NL_CheckSolution( UCellLink LK0, UCellLink LKnxt, Stack<UCellLink> SolStack, Bit81 UsedCells ){ 
            bool SolFound=false;
            int SolType = CeLKMan.Check_CellCellSequence(LKnxt,LK0)? 1: 2; //1:Continuous 2:DisContinuous

            //==================== continuous ====================
            if( SolType==1 ){
                //----- Change WeakLink to StrongLink 
                List<UCellLink> SolLst=SolStack.ToList();
                Bit81 UsedCellsT = UsedCells|(new Bit81(LK0.rc1));

                foreach( var L in SolLst ){
                    int noB=1<<L.no;
                    foreach( var P in pBOARD.IEGetCellInHouse(L.h,noB) ){
                        if( UsedCellsT.IsHit(P.rc) ) continue;
                        P.CancelB |= noB;
                        SolFound=true;
                    }
                }

                //=== S-S (There are no other digits)
                SolLst.Reverse();
                SolLst.Add(LK0);        
        
                var LKpre = SolLst[0];
                foreach( var LK in SolLst.Skip(1) ){
                    if( LKpre.type==1 && LK.type==1 ){ //S-S
                        UCell P=pBOARD[LK.rc1];
                        int noB = P.FreeB.DifSet((1<<LKpre.no)|(1<<LK.no));
                        string rcSt = " " + LK.rc1.ToRCString();
                        if(noB>0){ P.CancelB=noB; SolFound=true; }
                    }
                    LKpre = LK;
                }
                if( SolFound ) SolCode=2;
            }

            //==================== discontinuous ====================
            else if( SolType==2 ){
                UCell P=pBOARD[LK0.UCe1.rc];  //(for MultiAns code)
                int dcTyp= LK0.type*10+LKnxt.type;
                switch(dcTyp){
                    case 11: 
                        P.FixedNo=LK0.no+1; //Cell number determination
                        P.CancelB=P.FreeB.DifSet(1<<(LK0.no));
                        SolFound=true; //(1:Fixed）
                                break;
                    case 12: P.CancelB=1<<LKnxt.no; SolCode=2; SolFound=true; break;//(2:Exclude from candidates）
                    case 21: P.CancelB=1<<LK0.no; SolCode=2; SolFound=true; break;
                    case 22: 
                        if( LK0.no==LKnxt.no ){ P.CancelB=1<<LK0.no; SolFound=true; SolCode=2; }
                        break;
                }
            }

            if( SolFound ) return SolType;

            return -1;
        }

        private void _NL_SolResult( UCellLink LK0, UCellLink LKnxt, Stack<UCellLink> SolStack, int SolType ){
            string st="", stT="", stT2="";

            List<UCellLink> SolLst=SolStack.ToList();
            SolLst.Reverse();
            SolLst.Add(LK0);

            UCell P0 = pBOARD[LK0.rc1];
            foreach( var LK in SolLst ){
                int noB=(1<<LK.no);
                UCell P1=pBOARD[LK.rc1], P2=pBOARD[LK.rc2];
                if( SolType==1 || (SolType==2 && P2!=P0) )  P2.Set_CellBKGColor(SolBkCr);
                if(LK.type==S){ P1.Set_CellDigitsColor_noBit(noB,AttCr); P2.Set_CellDigitsColor_noBit(noB,AttCr3); }
                else{           P2.Set_CellDigitsColor_noBit(noB,AttCr); P1.Set_CellDigitsColor_noBit(noB,AttCr3); }
            }
            if( SolType==2 ) P0.Set_CellBKGColor(SolBkCr2);    


            if( SolType==1 ){      // ========== continuous ==========
                st = $"Nice Loop(Continuous)";
                var no_rcLst = new string[9];
                var LKpre = SolLst[0];
                foreach( var LK in SolLst.Skip(1) ){
                    if( LKpre.type==1 && LK.type==1 ){ //S-S
                        UCell P=pBOARD[LK.rc1];
                        int noB = P.FreeB.DifSet((1<<LKpre.no)|(1<<LK.no));
                        string rcSt = " " + LK.rc1.ToRCString();
                        if(noB>0){ 
                            foreach( var no in noB.IEGet_BtoNo() ){
                                if (no_rcLst[no] is null ) no_rcLst[no] = "";
                                no_rcLst[no] += rcSt;
                            }
                            P.CancelB = noB;
                        }
                    }
                    LKpre = LK;
                }
                for( int no=0; no<9; no++ ){
                    if( no_rcLst[no] is null )  continue;
                    stT += " " + no_rcLst[no].ToString_SameHouseComp( $"#{no+1}" );
                }
                if( stT != "" ) stT = $"\r  {stT} is false";

                no_rcLst = new string[9];
                foreach( var P in pBOARD.Where(p=>p.CancelB>0) ){
                    foreach( var no in P.CancelB.IEGet_BtoNo() ){
                        if (no_rcLst[no] is null ) no_rcLst[no] = "";
                        no_rcLst[no] += $" {P.rc.ToRCString()}";
                    }
                }
                for( int no=0; no<9; no++ ){
                    if( no_rcLst[no] is null )  continue;
                    stT2 += " " + no_rcLst[no].ToString_SameHouseComp( $"#{no+1}" );
                }
                if( stT2 != "" ) stT = $"\r  {stT2} is false";
            }
            else{               // ========== discontinuous ==========
                st = $"Nice Loop(Discontinuous)";
                stT = $"\r {LK0.rc1.ToRCString()}";
                int dcTyp= LK0.type*10+LKnxt.type;
                switch(dcTyp){
                    case 11: stT+=$"#{(LK0.no+1)} is true";    break;  //S->S
                    case 12: stT+=$"#{(LKnxt.no+1)} is false"; break;  //S->W
                    case 21: stT+=$"#{(LK0.no+1)} is false";   break;  //W->S
                    case 22: stT+=$"#{(LK0.no+1)} is false";   break;  //W->W
                }
            }

            Result = st;
            string st2 = $"{st}\r{_ToRCSequenceString(SolStack)}";
            if( stT!="" ) st2 += $"\r{stT}";
            ResultLong = st2; 
        }
        private string _ToRCSequenceString( Stack<UCellLink> SolStack ){    
            if( SolStack.Count==0 ) return ("rc : -");
            List<UCellLink> SolLst=SolStack.ToList();
            SolLst.Reverse();

            UCellLink LK0=SolLst[0];
            UCell     P0 =pBOARD[LK0.rc1];
            string st = $" rc : {P0.rc.ToRCString()}";
            foreach( var LK in SolLst ){
                UCell  P1 = pBOARD[LK.rc2];
                string mk = (LK.type==1)? "=": "-";
                st += mk+(LK.no+1)+mk+$"[{P1.rc.ToRCString()}]";
            }
            return st;
        }

        private int ___NLCC=0;
        private void ___Debug_Print_NLChain( Stack<UCellLink> SolStack ){
            WriteLine( $"<{___NLCC++}> {_ToRCSequenceString(SolStack)}" );
        }
    }
}