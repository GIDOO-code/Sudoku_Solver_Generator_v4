using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Reflection.Metadata.Ecma335;

namespace GNPXcore{
    public partial class ALSTechGen: AnalyzerBaseV2{

        //ALS Chain is an algorithm that connects ALS into a loop in RCC.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page52.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //7....5..9.9.4.7.8...8.3.7..81.3.9.6...6...9...3.7.2.18..3.7.1...4.5.1.2.1..6....4
        //6..9....5.4.5.6.7...5.3.2...6.8.3.91..8...6..91.7.2.5...6.7.4...7.3.5.6.2....4..7
        //.9..4..6.4..15...2..6..91....4....7.36.....15.8....3....82..4..9...34..1.4..8..3.

        // ALS (Almost Locked Set)
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page26.html
         
        //private int _debugCC=0; 

        private bool break_ALS_Chain=false; //True if the number of solutions reaches the specified number.
        public bool ALS_Chain(){
            break_ALS_Chain=false;
            //_debugCC = 0;

			Prepare();
            if(ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=3) return false;
            ALSMan.QSearch_ALS2ALS_Link(true);                  //T:doubly+singly (F:only singly)
            
            for(int szCtrl=3; szCtrl<=12; szCtrl++ ){           //Search from small size ALS-Chain
                if( pAnMan.Check_TimeLimit() ) return false;

                var SolStack=new Stack<UALSPair>();
                foreach( var ALSHead in ALSMan.ALSLst.Where(p=>p.ConnLst!=null && !p.LimitF) ){
                    if( !ALSHead.singly )  continue;
                    bool limitF=false;
                    foreach( var LK0 in ALSHead.ConnLst ){  //LK0 : "Stem" of ALS-chain
                        SolStack.Push(LK0);
                        Bit81 rcUsed = new Bit81(LK0.rcUsed);
                        int szCtrlX = szCtrl - LK0.ALSpre.Size - LK0.ALSnxt.Size;    //Total ALS size controls chain size

                        //LK0:stem, LK0_2:front ALS, SolStack:Chain stack, szCtrlX:Size control,
                        //limitF:True=limit exceeded 
                        _Search_ALSChain( LK0, LK0, SolStack, rcUsed, szCtrlX, ref limitF ); //First Recursive Search
                        if( break_ALS_Chain )  return true;

                        SolStack.Pop();
                    }
                    if(!limitF) ALSHead.LimitF=true;　//When the solution is within the size limit, do not search by the next size
                }
            }
            return (SolCode>0);
        }

        private bool _Search_ALSChain( UALSPair LK0, UALSPair LKpre, Stack<UALSPair> SolStack, 
                                       Bit81 rcUsedPre, int szCtrl, ref bool limitF ){
            if( break_ALS_Chain ) return true;

            // ===== Recursive Search =====
            int nRccPre = LKpre.nRCC;
            foreach( var LKnxt in LKpre.ALSnxt.ConnLst.Where(p=>(p.nRCC!=nRccPre)) ){           
                UALS UAnxt = LKnxt.ALSnxt;                                //Next connected ALS
                if(!UAnxt.singly)  continue;
                int szCtrlNxt = szCtrl - UAnxt.Size;                      //Chain Size : Accumulation of ALS size
                if( szCtrlNxt<0 ){ limitF=true; return false; }           //Upper limit of ChainsSize has been exceeded
                if( (rcUsedPre&UAnxt.B81).IsNotZero() ) continue;         //Stop if the next node has been searched

                { //===== Extend the link and try ahead =====
                    SolStack.Push(LKnxt);           //Push the next Link onto the stack
                    Bit81 rcUsedNxt = new Bit81( rcUsedPre | UAnxt.B81); //Used cells in the current phase of recursive search

                    if( _CheckSolution_ALSChain( LK0, LKnxt, rcUsedNxt, SolStack ) ) return true;    //Check the solution
                    if( break_ALS_Chain ) return true;
                    if( _Search_ALSChain( LK0, LKnxt, SolStack, rcUsedNxt, szCtrlNxt, ref limitF) ) return true;  //Recursive call
                    SolStack.Pop();         //Pop:Return trial
                } 
            }
            return (SolCode>0);
        }

        private bool _CheckSolution_ALSChain( UALSPair LK0, UALSPair LKn, Bit81 rcUsed, Stack<UALSPair> SolStack ){
            int ElmBH = LK0.ALSpre.FreeB.BitReset(LK0.nRCC);    //non-RCC digit of First ALS. RCC(Restricted Common Candidate)
            int ElmBT = LKn.ALSnxt.FreeB.BitReset(LKn.nRCC);    //non-RCC digit of Last ALS
            int ElmB  = ElmBH&ElmBT;
            if( ElmB==0 ) return false;                         //if no common digits then Failure. 

            foreach( int Eno in ElmB.IEGet_BtoNo() ){           //Eno is one of common digits
                int EnoB=(1<<Eno);

                Bit81 Ez = new Bit81( LK0.ALSpre.UCellLst,EnoB );
                Ez |= new Bit81( LKn.ALSnxt.UCellLst,EnoB );

                Bit81 TBD = (new Bit81(pBOARD,EnoB)) - rcUsed;    //Exclude cells in the chain
                Bit81 B81rc = new Bit81();
                foreach( var rc in TBD.IEGet_rc() ){
                    if( (Ez-ConnectedCells[rc]).IsNotZero() )  continue;
                    SolCode=2;      //solution found
                    pBOARD[rc].CancelB |= EnoB;
                    B81rc.BPSet(rc);
                }
                
                if( B81rc.IsNotZero() ){
                    _SolResult_ALSChain( Eno, B81rc, SolStack );
                    if( __SimpleAnalyzerB__ )  return true;
                    if( !pAnMan.SnapSaveGP(pPZL) ){ break_ALS_Chain=true; return true; }
                }
            }

            return false;
        }
        private void _SolResult_ALSChain( int Eno, Bit81 B81rc, Stack<UALSPair> SolStack ){    
            string stRC = B81rc.ToString_SameHouseComp() + $" #{Eno+1}";
            int nc=0;

            if( SolInfoB ){
                int noB=0;
                Color cr, clrBlk = Colors.Black;
                var SSrev = SolStack.ToList();
                SSrev.Reverse();
                        //WriteLine( $"--stageNo:{stageNo}  _debugCC:{_debugCC++}" );

                string st = $"ALS Chain\n Stem : {stRC} ->";  
                foreach( var LKA in SSrev ){
                    noB = (1<<LKA.nRCC);
                    UALS UA = LKA.ALSpre;
                    cr = _ColorsLst[ (nc++)%_ColorsLst.Length ]; 
                   
                  //UA.UCellLst.IE_SetNoBBgColor( noB, AttCr, cr );
                    UA.UCellLst.IE_SetNoBBgColor( pBOARD, 0, clrBlk, cr );
                    st += $"\r ALS {nc}: {UA.ToStringRCN()} -> #{(LKA.nRCC+1)}";
                }
                var LKB = SSrev.Last();
                cr = _ColorsLst[ (nc++)%_ColorsLst.Length ];
              
              //LKB.ALSnxt.UCellLst.IE_SetNoBBgColor( 1<<Eno, AttCr, cr );
                LKB.ALSnxt.UCellLst.IE_SetNoBBgColor( pBOARD, 0, AttCr, cr );
                st += $"\r ALS {nc}: {LKB.ALSnxt.ToStringRCN()} -> #{(LKB.nRCC+1)}";

                st += $"\r Excluded cells#no : {stRC}";
                ResultLong = st;
            }
            Result = $"ALS Chain  Exclud {stRC} Chain Lng.:{nc}";
        }

    }
}