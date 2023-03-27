using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;
using System.Windows.Media;

using GIDOO_space;
using System.Data;
using System.Windows.Controls;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Net.NetworkInformation;
using System.Windows.Interop;

namespace GNPXcore{
    public partial class ALSTechGen: AnalyzerBaseV2{
     //  static public Bit81[]    pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }
        static public Bit81[]    pConnectedCells => AnalyzerBaseV2.ConnectedCells;
		private int stageNoMemo = -9;
        private Bit981 BP981;

        private List<RCN_Bit_N>  SolList = new List<RCN_Bit_N>();

        public ALSTechGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){
            this.pAnMan=pAnMan;
        }

		private void Prepare( int minSize=1 ){
			if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				ALSMan.Initialize();
				ALSMan.PrepareALSLinkMan( 1, minSize:minSize, setCondInfo:true);
                BP981 = new Bit981(pBOARD,eSet:false);
			}      
		}

        //ALS-Wing is an analysis algorithm using three ALS. It is the case of the next ALS Chain 3ALS.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page44.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //7.1..9..8.52...19..8...3.574.3.5.......2.1.......3.7.519.7...3..37...68.8..3..9.1
	    //2.9..3..8.17...63..8...6.259.5.6.......3.2.......9.3.114.6...9..53...48.7..4..2.6

        //...8...4....21...7...7.5981315..9..8.8....4....41.83.5..1.82.646.8...1...236.18.. 

        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page44.html   - XYZ-WingALS
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page51.html   - ALS XY-Wing


        public bool ALS_Wing( ){    // (Locked_ALS_Wing)
            Prepare();
            if( ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=2 ) return false;
            ALSMan.QSearch_Cell2ALS_Link();     //prepare cell-ALS link
            Bit81 BZero = new Bit81();


            for( int sz=2; sz<=4; sz++ ){    // sz:number of digits in the stem cell
                List<UCell> FBCX = pBOARD.FindAll(p=>p.FreeBC==sz);
                if( FBCX.Count==0 )  return false;

                foreach( var P0 in FBCX ){  // forcused cell
                    if( P0.FreeBC!=sz )  continue;

                    var LinkCellALSs = ALSMan.LinkCeAlsLst[P0.rc];
                    if( LinkCellALSs is null )  continue;
                  // ------------------------------------------------------------------------------------

                    int sizeALSs = LinkCellALSs.Count;
                    SolList.Clear();                        



                    int __szSearch = Math.Min( sizeALSs, 6 );
                    for( int szSearch=2; szSearch<=__szSearch; szSearch++ ){ 
                        // szSearch: number of ALSs linked with stem Cell
                        // 2:XYZ-Wing 3:XYZ-Wing 4:WXYZ-Wing 5:VWXYZ-Wing 6:UVWXYZ-Wing

                        var cmb = new Combination( sizeALSs , szSearch );
                        int nxt=int.MaxValue;
                        while( cmb.Successor(skip:nxt) ){
                            if( pAnMan.Check_TimeLimit() )  return false;
                            nxt=int.MaxValue;

                          // ============== Select ALS in Combination ==============
                            int   noBcommon = 0x1FF;                      // digits common to all ALS
                            bool  overlapB  = false; 
                            Bit81 usedCells = new Bit81();                // check for ALS overlap
                            var   LK_CeALSs = new List<LinkCellALS>();
                            for( int k=0; k<szSearch; k++ ){


                                var P = LinkCellALSs[ cmb.Index[k] ];
                                if( (usedCells&P.ALS.B81).IsNotZero() ) overlapB=true;
                                usedCells |= P.ALS.B81;
                                noBcommon &= P.ALS.FreeB;   
                                LK_CeALSs.Add(P);                         // ALSs
                            }
                            if( noBcommon==0 )  continue;


                          // ============== Search ==============
                            // ---------- Select noX (candidate for deletion) ----------
                            foreach ( int noX in noBcommon.IEGet_BtoNo() ){         // choose a common digit => noX

                                bool ConnectedB = (P0.FreeB & (1<<noX)) > 0;
                                if( ConnectedB && (P0.FreeBC-1)<szSearch )  continue;

                              //var rcCands = LK_CeALSs.Aggregate( new Bit81(all1:true), (p,lk)=> p & lk.ALS.B981con._BQ[noX] );

                                var rcCands = new Bit81(all1:true);
                                foreach( var lk in LK_CeALSs ){
                                  //if( (lk.ALS.FreeB&noXB) == 0 ) goto LNext_rcX; 
                                    if( lk.ALS.B981con is null )   goto LNext_rcX;
                                    rcCands &= lk.ALS.B981con._BQ[noX];
                                }

                                rcCands.BPReset(P0.rc);
                                if( rcCands.IsZero() )  continue;                  // no solution candidate

                                // ---------- Select rcX (candidate for deletion) ----------
                                Bit81 elmB = new Bit81();
                                foreach( int rcX in rcCands.IEGetRC() ){           // choose a stem cell => rcX
                                    if( (pBOARD[rcX].FreeB&(1<<noX)) == 0 )  continue;

                                    int FreeBX = P0.FreeB;
                                    if( ConnectedB &&  pConnectedCells[rcX].IsHit(P0.rc) )  FreeBX = FreeBX.BitReset(noX);

                                    // ---------- Select Cell_ALS Link ----------
                                    foreach( var LK in LK_CeALSs ){

                                        Bit981 B981influencer = LK.Func_Connected_ALS2Cell( BP981, rcX, noX );
                                        //If (rcX,#noX) is true, ALS changes to LS, and this function finds "false cells/digits".
                                        if(  B981influencer is null ) goto LNext_rcX;
                                       
                                        foreach( var noA in P0.FreeB.IEGet_BtoNo() ){
                                            if( B981influencer._BQ[noA] is null ) continue;
                                            if( B981influencer._BQ[noA].IsHit(P0.rc) )  FreeBX = FreeBX.BitReset(noA); // deleted
                                        }
                                    }

                                    if( FreeBX==0 ){        // All stem cell candidate digits are deleted.
                                        if( !__UniquCheck( sz, P0, sizeALSs, rcX, noX, cmb.Index) )  continue;   //exclude the ALS group in excess
                                        var RCNB = new RCN_Bit_N( sizeALSs, rcX, noX, cmb.Index );
                                        SolList.Add( RCNB );     
                                        elmB.BPSet(rcX);     // ===== found the solution. Temporarily store solutions. =====
                                    }
                                }

                                if( elmB.IsNotZero() ){         
                                    //Report multiple solutions(accumulated solutions).
                                    SolCode = 2;


                                    foreach( var rc in elmB.IEGetRC()){ pBOARD[rc].CancelB = 1<<noX; }
         
                                    if( SolInfoB ){
                                        string SolMsg = $"ALS_Wing[{szSearch}D] Stem Cell: {P0.rc.ToRCString()} Eliminated #{noX+1}";
                                        Result = SolMsg;
                                        SolMsg = $"ALS_Wing";
                                        SolMsg += (ConnectedB? " Connected": "") + (overlapB? " overlapB81P0Hg": "") ;
                                        SolMsg += $"\n  Stem Cell: {P0.rc.ToRCString()}";
                                        ResultLong = SolMsg + ALS_Wing_SolResult( P0, LK_CeALSs, noX, elmB );
                                    }

                                    if( __SimpleAnalyzerB__ )  return true;
                                    if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                                    int noXB = 1<<noX;
                                    foreach( var E in pBOARD.Where(p=>(p.FreeB&noXB)>0) ) E.CancelB=0;
                                }

                             LNext_rcX:
                                continue;
                            }
                        }
                    }
                    // if( SolList.Count>0 )  SolListToWrite( sz, P0 );

                }
            }
            return false;

            bool __UniquCheck( int sz, UCell P0,  int sizeALSs, int rcX, int noX, int[] indxLst ){
                if( SolList.Count <= 0 ) return true;    // --- unique ---
                int szL = indxLst.Length;

                for( int szM=1; szM<=szL; szM++ ){ 
                    Combination cmbS = new Combination(szL, szM);
                    while( cmbS.Successor() ){
                        var q = new RCN_Bit_N(sizeALSs, rcX, noX);
                        for( int k=0; k<szM; k++ ) q.BPSet(indxLst[ cmbS.Index[k] ]);
                        if( SolList.FindIndex(p=>p==q) >= 0 ){
                                // SolListToWrite(sz,p0);
                                // WriteLine( $"**Hit:{q} / {string.Join(" ",indxLst)}" );
                            return false; // --- not unique ---
                        }
                    }
                }

                return true;    // --- unique ---
            }

            void SolListToWrite( int sz, UCell P0 ){
                WriteLine( $"\n Solution_UniquCheck  sz:{sz} P0.rc:{P0.rc.ToRCString()} SolList:{SolList.Count}" );
                int nx=0;
                SolList.ForEach( p => WriteLine( $"SolList[{nx++}] {p.ToString()}" ) );
            }
        }

        private string ALS_Wing_SolResult( UCell P0, List<LinkCellALS> LK_CeALSs, int noX, Bit81 elmB ){
            int noXB = 1<<noX;
            P0.Set_CellColorBkgColor_noBit(P0.FreeB.DifSet(noXB), AttCr3, SolBkCr4);
            P0.Set_CellColorBkgColor_noBit(noXB, AttCr, SolBkCr4 );

            int kx = 0;
            string msg = "";
            foreach( var LK in LK_CeALSs){
                Color Cr = _ColorsLst[++kx];
                foreach( var uc in LK.ALS.UCellLst){
                    uc.Set_CellColorBkgColor_noBit(uc.FreeB.DifSet(noXB), AttCr3, Cr);
                    uc.Set_CellColorBkgColor_noBit(noXB, AttCr, Cr);
                }

                string stT = "";
                foreach( var P in LK.ALS.UCellLst) stT += " " + P.rc.ToRCString();
                msg += $"\n  ALS_{kx}: {stT.ToString_SameHouseComp()} #{LK.ALS.FreeB.ToBitStringNZ(9)}";
            }

            Bit81 overlapB = new Bit81();
            foreach( var P in pBOARD.Where(p=>p.FreeB>0 && p.ECrLst!=null) ){
                if( P.ECrLst.Count<=1 )  continue;
                var G = P.ECrLst.GroupBy(p=>p.ClrCellBkg).ToList();
                if( G.Count(q=> ((Color)q.Key)!=Colors.Black) >= 2 ) overlapB.BPSet(P.rc);
            }
            if( overlapB.IsNotZero() ){
                string stT = "";
                foreach( var rc in overlapB.IEGetRC() ) stT += " " + rc.ToRCString();
                msg += $"\n   (ALS overlapB81P0Hg cells: {stT.ToString_SameHouseComp()})";
            }

            string st = $"   {msg}\r  Eliminated: {elmB.ToString_SameHouseComp()} #{noX+1}"; 
            return st;
        }

    #region Bit_N management
        private class RCN_Bit_N: Bit_N{
            public int   rc;
            public int   no;

            public RCN_Bit_N() { }

            public RCN_Bit_N( int bitNsize, int rc, int no): base(bitNsize){
                this.rc = rc;
                this.no = no;
            }

            public RCN_Bit_N( int bitNsize, int rc, int no, int[] index ): base( bitNsize, index ){ 
                this.rc = rc;
                this.no = no;
            }

            public override bool Equals( object B ){
                try{
                    var  Bx = B as RCN_Bit_N;
                    bool b = (this.rc==Bx.rc)  && (this.no!=Bx.no) && ((Bit_N)this==(Bit_N)B);
                    return b;
                }
                catch (NullReferenceException ex) { WriteLine(ex.Message + "\r" + ex.StackTrace); return false; }
            }

            public override int GetHashCode() { 
                var w = base.GetHashCode() ^ (rc<<7) ^ (no<<3);
                return  w;
            }

            public override string ToString( ){
                string st = $"rc:{rc.ToRCString()} #{no+1} _BQ:{base.ToString()}";
                return st;
            }
            static public bool operator ==(RCN_Bit_N A, RCN_Bit_N B){
                try{
                    bool b = (A.rc==B.rc) && (A.no==B.no) && (((Bit_N)A)==((Bit_N)B));
                    return b;
                }
                catch (NullReferenceException ex) { WriteLine(ex.Message + "\r" + ex.StackTrace); return false; }
            }
            static public bool operator !=(RCN_Bit_N A, RCN_Bit_N B){
                try{
                    bool b = (A.rc!=B.rc) || (A.no!=B.no) || (((Bit_N)A)!=((Bit_N)B) );
                    return b;
                }
                catch (NullReferenceException ex) { WriteLine(ex.Message + "\r" + ex.StackTrace); return false; }
            }

        }
    #endregion


    }
}