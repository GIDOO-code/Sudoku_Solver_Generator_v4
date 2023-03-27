using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Navigation;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Windows.Interop;

namespace GNPXcore{
    public partial class ALSTechGen: AnalyzerBaseV2{

        //DeathBlossom is an algorithm based on the arrangement of ALS.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page53.html
        
        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //...8...4....21...7...7.5981315..9..8.8....4....41.83.5..1.82.646.8...1...236.18..
        //..2956.485.6....9.4...7.5...538...1.2.......6.1...582...1.8...2.9....1.582.4316..
        //285..91.47.3..1...16..24.7........8...2.4.6.7597..84..4...92..3..84....29...8.74.
        //594..26.31.8..6...26..39.8........2...6.5.4.7321..48..4...27..8..38....28...9.76.

        //1..89..4.8..21...7...7.59813154.9..8.8.5..41...41.8395..1382.646.89..1...236.18.9  for develop(1)
        //1.2956.485.6...29.4.9.7.56..538...1.248....56.17..582.761589432394...185825431679  for develop(2)
        //285..91.4743..1...169.24.7.6.4....8.8.2.4.6.7597..84.14...928.33.84....292..8.74.  for develop(3,4)

        private bool break_ALS_DeathBlossom=false; //True if the number of solutions reaches the specified number.


        public bool ALS_DeathBlossom() => ALS_DeathBlossomEx( connected:false );

        public bool ALS_DeathBlossomEx() => ALS_DeathBlossomEx( connected:true );


        public bool ALS_DeathBlossomEx( bool connected=false ){
            break_ALS_DeathBlossom=false;
			Prepare();
            if( ALSMan.ALSLst==null || ALSMan.ALSLst.Count<=2 ) return false;
            ALSMan.QSearch_Cell2ALS_Link();

            for(int sz=2; sz<=4; sz++){     //Size 5 and over ALS DeathBlossom was not found?
                if(_ALS_DeathBlossomSub( sz, connected:connected )) return true;
                if( break_ALS_DeathBlossom )  return true;
            }
            return (SolCode>0);
        }



        private bool _ALS_DeathBlossomSub( int sz, bool connected=false ){
            int szM= (connected? sz-1: sz);
            foreach( var SC in pBOARD.Where(p=>p.FreeBC==sz) ){                       //Stem Cell
                if(pAnMan.Check_TimeLimit()) return false;

                List<LinkCellALS> LinkCeAlsLst=ALSMan.LinkCeAlsLst[SC.rc];
                if( LinkCeAlsLst==null || LinkCeAlsLst.Count<sz) continue;
                        //int mx=0;
                        //LinkCeAlsLst.ForEach( X => WriteLine( $"#{mx++} {X}") );

                int nxt=0, PFreeB=SC.FreeB;
                var cmb = new Combination( LinkCeAlsLst.Count ,szM );               //Select szM ALSs in Combination
                while( cmb.Successor(skip:nxt) ){
                    nxt = szM;

                    int FreeB=SC.FreeB, AFreeB=0x1FF;
                    for( int k=0; k<szM; k++ ){
                        nxt = k;
                        var LK = LinkCeAlsLst[cmb.Index[k]];                        //Link[cell-ALS]
                        if( (FreeB&(1<<LK.nRCC)) ==0 ) goto LNxtCmb;               
                        FreeB = FreeB.BitReset(LK.nRCC);                            //nRCC:RCC of stemCell-ALS                      
                        AFreeB &= LK.ALS.FreeB; 
                        if( AFreeB == 0 )  goto LNxtCmb;
                    }



                    // =============== connected=true ===============
                    Bit81 E = new Bit81();
                    if( connected ){
                        if( FreeB.BitCount()!=1 || (FreeB&AFreeB)==0 )  continue;
                        int no  = FreeB.BitToNum();
                        int noB = FreeB;

                        Bit81 Ez = new Bit81();
                        for( int k=0; k<szM; k++ ){
                            var ALS = LinkCeAlsLst[cmb.Index[k]].ALS;
                            var UClst = ALS.UCellLst;
                            foreach( var P in UClst.Where(p=>(p.FreeB&noB)>0) )  Ez.BPSet(P.rc);
                        }

                        foreach( var P in ConnectedCells[SC.rc].IEGet_rc().Select(rc=>pBOARD[rc]) ){
                            if( (P.FreeB&noB)==0 ) continue;
                            if( (Ez-ConnectedCells[P.rc]).IsZero() ){ P.CancelB=noB; E.BPSet(P.rc); SolCode=2;  }
                        }
                        if(SolCode<1) continue;
                        
                        var LKCAsol=new List<LinkCellALS>();
                        Array.ForEach( cmb.Index,nx => LKCAsol.Add(LinkCeAlsLst[nx]) );
                        _DeathBlossom_SolResult( LKCAsol, SC, no, E, connected );

                        if( __SimpleAnalyzerB__ )       return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ break_ALS_DeathBlossom=true; return true; }

                    }



                    // =============== connected=false ===============
                    else if( FreeB==0 && AFreeB>0 ){
                        AFreeB = AFreeB.DifSet(SC.FreeB);
                        foreach( var no in AFreeB.IEGet_BtoNo() ){
                            int noB=(1<<no);
                            Bit81 Ez = new Bit81();
                            for( int k=0; k<sz; k++ ){
                                var ALS = LinkCeAlsLst[cmb.Index[k]].ALS;
                                var UClst = ALS.UCellLst;
                                foreach( var P in UClst.Where(p=>(p.FreeB&noB)>0) )  Ez.BPSet(P.rc);
                            }

                            foreach( var P in pBOARD.Where(p=>(p.FreeB&noB)>0) ){
                                if( (Ez-ConnectedCells[P.rc]).IsZero() ){ P.CancelB=noB; E.BPSet(P.rc); SolCode=2; }
                            }
                            if( SolCode<1 ) continue;
                        
                            var LKCAsol = new List<LinkCellALS>();
                            Array.ForEach( cmb.Index,nx=> LKCAsol.Add(LinkCeAlsLst[nx]) );
                            _DeathBlossom_SolResult( LKCAsol, SC, no, E, connected);

                            if( __SimpleAnalyzerB__ )       return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ){ break_ALS_DeathBlossom=true; return true; }
                        }
                    }
                
                LNxtCmb:
                    continue;
                }
            }
            return false;
        }

        private void _DeathBlossom_SolResult( List<LinkCellALS> LKCAsol, UCell SC, int no, Bit81 E, bool connected=false ){
            string st0 = "ALS Death Blossom";
            if(connected) st0 += "Ex";

            Color cr = _ColorsLst[0];////Colors.Gold;
            SC.Set_CellColorBkgColor_noBit(SC.FreeB,AttCr3,cr);
            string st = $"\r       Stem : {SC.rc.ToRCString()} #{SC.FreeB.ToBitStringNZ(9)}";

            bool  overlap = false;
            Bit81 OV = new Bit81();
            int   k=0, noB=(1<<no);
            foreach( var LK in LKCAsol ){
                int noB2 = 1<<LK.nRCC;
                cr = _ColorsLst[++k];
                LK.ALS.UCellLst.ForEach( p=> { 
                    UCell P = pBOARD[p.rc];
                    P.Set_CellColorBkgColor_noBit(noB,AttCr,cr);
                    P.Set_CellColorBkgColor_noBit(noB2,AttCr3,cr);
                    if( OV.IsHit(P.rc) ) overlap=true;
                    OV.BPSet(P.rc);
                } );
                st += $"\r   -#{(LK.nRCC+1)}-ALS{k} : {LK.ALS.ToStringRCN()}";
            }

            st += $"\r eliminated : { E.ToString_SameHouseComp()} #{no+1}";

            if( overlap ) st0+=" [overlap]";
            if( connected )  st0+=" [connected]";
            st0 = st0.Replace("] [","," );
            Result = st0;
            if( SolInfoB ) ResultLong=st0+st;
        }
    }
}