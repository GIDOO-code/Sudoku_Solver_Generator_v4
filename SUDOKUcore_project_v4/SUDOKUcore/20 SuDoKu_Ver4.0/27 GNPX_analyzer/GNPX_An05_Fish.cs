using System;
using System.Linq;
using static System.Diagnostics.Debug;
using GIDOO_space;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;

namespace GNPXcore{
    public partial class FishGen: AnalyzerBaseV2{
        public FishGen( GNPX_AnalyzerMan pAnMan ): base(pAnMan){ }

        public bool break_Finned_Fish=false;
        //=======================================================================
        //Fish:
        // Understand this algorithm, you need to know BaseSet and CoverSet.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page34.html

        public bool XWing()     => Fish_Basic(2);
        public bool SwordFish() => Fish_Basic(3);
        public bool JellyFish() => Fish_Basic(4);
        public bool Squirmbag() => Fish_Basic(5);    //complementary to 4D 
        public bool Whale()     => Fish_Basic(6);    //complementary to 3D 
        public bool Leviathan() => Fish_Basic(7);    //complementary to 2D 

        //FinnedFish
        public bool FinnedXWing()     => Fish_Basic(2,fin:true);
        public bool FinnedSwordFish() => Fish_Basic(3,fin:true);
        public bool FinnedJellyFish() => Fish_Basic(4,fin:true);
        public bool FinnedSquirmbag() => Fish_Basic(5,fin:true);
        public bool FinnedWhale()     => Fish_Basic(6,fin:true);
        public bool FinnedLeviathan() => Fish_Basic(7,fin:true);

        //81........27.3149.......718.9.34.....7.....6.....96.2.182.......4512.98........41
        //81.....32.2783149.......7182963481..478.1.369..1796824182.....3.4512.98.....8.241

      //-----------------------------------------------------------------------
        // Basic Fish
        public bool Fish_Basic( int sz, bool fin=false ){

            break_Finned_Fish=false;
            const int _rowSel=0x1FF, _colSel=0x1FF<< 9;
            for( int no=0; no<9; no++ ){
                foreach( var _ in ExtFishSub( sz, no, 18, _rcbSel, _rcbSel, FinnedFlag:fin) ){
                        if( pAnMan.Check_TimeLimit() ) return false;
                        if( __SimpleAnalyzerB__ )   return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ return true; }
                }
                if( pAnMan.Check_TimeLimit() ) return false;
                if( break_Finned_Fish ) return true;
            }
            return (SolCode>0);
        }   

#if RegularVersion    
      //-----------------------------------------------------------------------
        // Frankenn/MutantFish
        private const int _rcbSel=0x7FFFFFF;
        public bool FrankenMutantFish( ){       
            for( int sz=2; sz<=4; sz++ ){   //no fin: max size is 4
                for( int no=0; no<9; no++ ){
                    foreach( var _ in ExtFishSub( sz, no, 27, _rcbSel, _rcbSel, FinnedFlag:false) ){
                        if( pAnMan.Check_TimeLimit() ) return false;
                        if( __SimpleAnalyzerB__ )   return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ return true; }
                    }
                    if( pAnMan.Check_TimeLimit() ) return false;
                    if( break_Finned_Fish ) return true;
                }
            }
            return false;
        }
        // Finned Frankenn/MutantFish
        public bool FinnedFrankenMutantFish( ){
            for( int sz=2; sz<=7; sz++ ){   //Finned: max size is 7 (5:Squirmbag 6:Whale 7:Leviathan)
              //if( sz>=6 )  WriteLine( $"break_Finned_Fish:{break_Finned_Fish}" );
                for( int no=0; no<9; no++ ){
                    foreach( var _ in ExtFishSub( sz, no, 27, _rcbSel, _rcbSel, FinnedFlag:true) ){
                        if( pAnMan.Check_TimeLimit() ) return false;
                        if( __SimpleAnalyzerB__ )   return true;
                        if( !pAnMan.SnapSaveGP(pPZL) ){ return true; }
                    }
                    if( pAnMan.Check_TimeLimit() ) return false;
                    if( break_Finned_Fish ) return true;
                }
            }
            return false;
        }
#endif

      //-----------------------------------------------------------------------
        private FishMan FMan=null;

        public IEnumerable<bool> ExtFishSub( int sz, int no, int FMSize, int BasesetFilter, int CoverSetFilter, bool FinnedFlag, bool _Fdef=true ){       
            int noB=(1<<no);
            bool extFlag = (sz>=3 && ((BasesetFilter|CoverSetFilter).BitCount()>18));
            if(_Fdef) FMan = new FishMan(this,FMSize,no,sz,extFlag);

            // ===== select BaseSet =====
            foreach( var Bas in FMan.IEGet_BaseSet(BasesetFilter,FinnedFlag:FinnedFlag )){ 
                if( pAnMan.Check_TimeLimit() )  yield break;

                // ===== select CoverSet =====
                foreach( var Cov in FMan.IEGet_CoverSet(Bas,CoverSetFilter,FinnedFlag) ){
                    Bit81 FinB81 = Cov.FinB81;
                    Bit81 ELM=null;
                    var FinZeroB = FinB81.IsZero();

                    //===== no Fin =====
                    if( !FinnedFlag && FinZeroB ){         
                        if( !FinnedFlag && (ELM=Cov.CoverB81-Bas.BaseB81).Count>0 ){                      
                            foreach( var P in ELM.IEGetUCell_noB(pBOARD,noB) ){ P.CancelB=noB; SolCode=2; }
                            if(SolCode>0){              //solved!
                                if( SolInfoB ){
                                    _FishResult(no,sz,Bas,Cov,(FMSize==27)); //FMSize 18:regular 27:Franken/Mutant
                                }
                                yield return true;
                            }
                        }
                    }

                     //===== Finned ===== 
                    else if( FinnedFlag && !FinZeroB ){    //===== Finned ===== 
                        Bit81 Ecand=Cov.CoverB81-Bas.BaseB81;
                        ELM=new Bit81();
                        foreach( var P in Ecand.IEGetUCell_noB(pBOARD,noB) ){
                            if( (FinB81-ConnectedCells[P.rc]).Count==0 ) ELM.BPSet(P.rc);
                        }
                        if(ELM.Count>0){    //there are cells/digits can be excluded                        
                            foreach( var P in ELM.IEGet_rc().Select(p=>pBOARD[p]) ){ P.CancelB=noB; SolCode=2; }   
                            if(SolCode>0){  //solved!
                                if( SolInfoB ){
                                    _FishResult(no,sz,Bas,Cov,(FMSize==27)); //FMSize 18:regular 27:Franken/Mutant
                                }
                                yield return true          ;
                            }
                        }
                    }
                    continue;
                }
            }
            yield break;       
        }

      //-----------------------------------------------------------------------
        private void _FishResult( int no, int sz, UFish Bas, UFish Cov, bool FraMut ){
            int   HB=Bas.HouseB, HC=Cov.HouseC;
            Bit81 PB=Bas.BaseB81, PFin=Cov.FinB81; 
            Bit81 EndoFin=Bas.EndoFinB81, CnaaFin=Cov.CannFinB81;
            string[] FishNames = { "Xwing","SwordFish","JellyFish","Squirmbag","Whale", "Leviathan" };
    
            PFin-=EndoFin;
            try{
                int noB=(1<<no);                 
                (PB-PFin).IE_SetNoBBgColor( pBOARD, noB, AttCr, SolBkCr );
                PFin.IE_SetNoBBgColor(      pBOARD, noB, AttCr, SolBkCr2 );
                EndoFin.IE_SetNoBBgColor(   pBOARD, noB, AttCr, SolBkCr3 );
                CnaaFin.IE_SetNoBBgColor(   pBOARD, noB, AttCr, SolBkCr3 );

                string msg = $"\r     Digit: #{no+1}";                 
                msg += $"\r   BaseSet: {HB.HouseToString()}";  //+"#"+(no+1);
                msg += $"\r  CoverSet: {HC.HouseToString()}";  //+"#"+(no+1);
                string msg2 = $" #{no+1} {HB.HouseToString().Trim()}/{HC.HouseToString().Trim()}";
 
                string FinmsgH="", FinmsgT="";
                if( PFin.Count>0 ){
                    FinmsgH = "Finned ";
                    msg += $"\r    FinSet: {PFin.ToString_SameHouseComp()}";                
                }
                 
                if( EndoFin.IsNotZero() ){
                    FinmsgT = " with Endo Fin";
                    msg += $"\r  Endo Fin: {EndoFin.ToString_SameHouseComp()}";
                }

                if( CnaaFin.IsNotZero() ){
                    FinmsgH = "Cannibalistic ";
                    if( PFin.Count>0 ) FinmsgH = "Finned Cannibalistic ";
                    msg += $"\r  Cannibalistic: {CnaaFin.ToString_SameHouseComp()}";
                }

                string Fsh = FishNames[sz-2];
                if(FraMut) Fsh = "Franken/Mutant "+Fsh;
                Fsh = FinmsgH+Fsh+FinmsgT;
                ResultLong = Fsh+msg;  
                Result = Fsh.Replace("Franken/Mutant","F/M")+msg2;
            }
            catch( Exception ex ){
                WriteLine(ex.Message+"\r"+ex.StackTrace);
            }
        }
    }  
}