using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using static System.Diagnostics.Debug;
using System.Security.Cryptography;

namespace GNPXcore{

//in development

    //Kraken Fish is an algorithm that connects ALS into a loop in RCC.
    // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page38.html

    //Kraken (Finned) Fish is an algorithm that combines Super Link with Fish. 
    //Kraken (Finned) Fish is an analysis algorithm that extends the effectiveness of link to multiple links connections.
    //For links, super links (inter-cell links, cell group links, ALS links) are used.

    //Duplicate solutions occur in Kraken Franken. Omitted the duplicate Solutions in the code below.
    //  pAnMan.SnapSaveGP(pPZL,OmitteDuplicateSolutionB:true)

    //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
    //.38.6...96....93..2..43...1..61..9355.3.8.1...4........8.65..13...8..5.63.59..827
    //.2...783..47.2...13..1....7....38.15...5.4...58.79....6....2..82...8.57..793...6. 


    public partial class GroupedLinkGen: AnalyzerBaseV2{

        public bool KrakenFishEx( ){
			Prepare();
			for(int sz=2; sz<5; sz++ ){
				for(int no=0; no<9; no++ ){

        		    foreach( var _ in ExtKrFishSubEx_ver2( sz, no, 27, FinnedFlag:false) ){
                        if( __SimpleAnalyzerB__ )  return true;
						if( !pAnMan.SnapSaveGP(pPZL,OmitteDuplicateSolutionB:true) )  return true;
						extResult = "";
                    }
				}
            }
            return false;
        }


        public bool KrakenFinnedFishEx( ){
			Prepare();
            for(int sz=2; sz<5; sz++ ){
				for(int no=0; no<9; no++ ){

        		    foreach( var _ in ExtKrFishSubEx_ver2( sz, no, 27, FinnedFlag:true) ){
                        if( __SimpleAnalyzerB__ )  return true;
						if( !pAnMan.SnapSaveGP(pPZL,OmitteDuplicateSolutionB:true) )  return true;
						extResult = "";
                    }
				}
            }
            return false;
        }
        public IEnumerable<bool> ExtKrFishSubEx_ver2( int sz, int no, int FMSize, bool FinnedFlag ){
			int BasesetFilter=0x7FFFFFF, CoverSetFilter=0x7FFFFFF;
            int noB=(1<<no);
            Bit81 BDL_no81 = new Bit81( pBOARD, noB );

			FishMan FMan = new FishMan(this,FMSize,no,sz,(sz>=3));
            foreach( var Bas in FMan.IEGet_BaseSet(BasesetFilter,FinnedFlag:FinnedFlag) ){   //Generate BaseSet
                        //if( Bas.HouseB.HouseToString() != "r12" )  continue;   //for debugging

                foreach( var Cov in FMan.IEGet_CoverSet(Bas,CoverSetFilter,FinnedFlag) ){    //Generate CoverSet  
                        //if( Cov.HouseC.HouseToString() != "b12" )  continue;   for debugging
                    Bit81 FinB81 = Cov.FinB81;

                    if( FinnedFlag == FinB81.IsZero() )  continue;      // ""finned" designation?  "With fin"?
							//WriteLine( $"dbCC:{dbCC} \rbas:{Bas.BaseB81}\rCov:{Cov.CoverB81}" );  //for debugging
							//WriteLine( $"Bas.HouseB:{Bas.HouseB.ToBitString27()}");               //for debugging
							//WriteLine( $"Cov.HouseB:{Cov.HouseC.ToBitString27()}");               //for debugging
							
                    Bit81 cand81 = BDL_no81 - (Bas.BaseB81 | Cov.CoverB81 | Cov.FinB81);
                            //WriteLine( cand81 );                      //for debugging

                    //======================= house to prove =======================
                    string krfSolMsg="";
					foreach( var HouseCover in Cov.HouseB.IEGet_BtoNo(27) ){        // Choose a CoverSet house
                        Bit81 testCells = (Cov.CoverB81 & HouseCells[HouseCover]) | FinB81; //Success if all cells in testCells can be set to false


                        //======================= Solution candidate Cell #digit =======================  
                        foreach( var P in cand81.IEGetRC().Select(rc=>pBOARD[rc]) ){   // Cell(P) under test
                            foreach( var noZ in P.FreeB.IEGet_BtoNo() ){             // digit under test
                        
                                //======================= test =======================
                                // "pSprLKsMan.get_L2SprLKEx":
                                // Specify cells and digits.
                                // If this is true, find the cells and digits that prove true in the link concatenation.
                                // Similarly, find cells/digits that are proved to be false.
                                // The links use inter-cell links, ALS-links and AIC-links.

                                USuperLink USLK = pSprLKsMan.get_L2SprLKEx( P.rc, noZ, FullSearchB:true, DevelopB:false );

                                Bit81 E = testCells - USLK.Qfalse[no];Å@                 // cells proved false
                                if( E.IsNotZero() )  continue;
                                // proved. success!

								P.CancelB |= 1<<noZ;
								SolCode=2;

								if( SolInfoB ){
                                    string _krfMsgEx0 = $"{P.rc.ToRCString()} #{(noZ+1)} is false.";
									string _krfMsgEx  = _KrFish_FishResultEx( no, sz, Bas, Cov, _krfMsgEx0 );
                                    string _krfMsgEx2 = $"\r{_krfMsgEx}  {_krfMsgEx0}";
                                    krfSolMsg += _krfMsgEx2;
									foreach( var rc in testCells.IEGet_rc() ){
                                        string stW = _ToHouseName(HouseCover) + " ";
                                        stW += pSprLKsMan._GenMessage2false( USLK, pBOARD[rc], no );
                                        if( stW != "" )  krfSolMsg += "\r"+stW;
									}

                                    if( FinnedFlag ){
                                        foreach( var Q in FinB81.IEGetRC().Select( rc => pBOARD[rc]) ){ 
                                            string stW = $"Fin {Q.rc.ToRCString()}";
                                            stW += pSprLKsMan._GenMessage2false( USLK, Q, no );
                                        }
                                    }
								}

                                yield return true;
                            }   
                        }
                    }
                }
            }
            yield break;


            string _ToHouseName( int h ){
                string st="";
                switch(h/9){
                    case 0: st="   row "; break;
                    case 1: st="Column "; break;
                    case 2: st=" block "; break;
                }
                st += ((h%9)+1).ToString();
                return st;
            }
        }


        private string _KrFish_FishResultEx( int no, int sz, UFish Bas, UFish Cov, string msg0 ){
            int   HB=Bas.HouseB, HC=Cov.HouseC;
            Bit81 PB=Bas.BaseB81, PFin=Cov.FinB81; 
            Bit81 EndoFin=Bas.EndoFinB81, CnaaFin=Cov.CannFinB81;
            string[] FishNames = { "Xwing","SwordFish","JellyFish","Squirmbag","Whale", "Leviathan" };
    
            PFin-=EndoFin;
            try{
                int noB=(1<<no);    
                PB.IE_SetNoBBgColor( pBOARD, noB, AttCr,SolBkCr);
                PFin.IE_SetNoBBgColor( pBOARD, noB, AttCr,SolBkCr2);
                EndoFin.IE_SetNoBBgColor( pBOARD, noB, AttCr,SolBkCr3);
                CnaaFin.IE_SetNoBBgColor( pBOARD, noB, AttCr,SolBkCr3);

                string msg = "\r     Digit: " + (no+1);                 
                msg += "\r   BaseSet: " + HB.HouseToString();
                msg += "\r  CoverSet: " + HC.HouseToString();;
                string msg2=$" #{(no+1)} BaseSet:{HB.HouseToString().Replace(" ","")} CoverSet:{HC.HouseToString().Replace(" ","")}";
 
                string FinmsgH="", FinmsgT="";
                if(PFin.Count>0){
                    FinmsgH = "Finned ";
                    string st="";
                    foreach( var rc in PFin.IEGet_rc() ) st += " "+rc.ToRCString();
                    msg += "\r    FinSet: "+st.ToString_SameHouseComp();
                
                }
                 
                if( EndoFin.IsNotZero() ){
                    FinmsgT = " with Endo Fin";
                    string st="";
                    foreach( var rc in EndoFin.IEGet_rc() ) st += " "+rc.ToRCString();
                    msg += "\r  Endo Fin: "+st.ToString_SameHouseComp();
                }

                if( CnaaFin.IsNotZero() ){
                    FinmsgH = "Cannibalistic ";
                    if( PFin.Count>0 ) FinmsgH = "Finned Cannibalistic ";
                    string st="";
                    foreach( var rc in CnaaFin.IEGet_rc() ) st += " "+rc.ToRCString();
                    msg += "\r  Cannibalistic: "+st.ToString_SameHouseComp();
                }

                string Fsh = FishNames[sz-2];
				int bf=0, cf=0;
				for(int k=0; k<3; k++ ){
					if( ((Bas.HouseB>>(k*9))&0x1FF)>0 ) bf |= (1<<k);
					if( ((Cov.HouseC>>(k*9))&0x1FF)>0 ) cf |= (1<<k);
				}
                if((bf+cf)>3) Fsh = "Franken/Mutant " + Fsh;
                Fsh = "Kraken "+FinmsgH+Fsh+FinmsgT;
                ResultLong = Fsh+msg+"\r"+msg0;  
                string _krfMsgEx = Fsh.Replace("Franken/Mutant","F/M")+msg2;
				Result = _krfMsgEx.Replace("BaseSet","Base").Replace("CoverSet","Cover");

                return _krfMsgEx;
            }
            catch(Exception ex){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }

            return "error in _KrFish_FishResultEx";
        }

        private void __Dev_PutDBLEx( string dir, string fName, bool append ){
            if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            using( var fp=new StreamWriter(dir+@"\"+fName,append:append,encoding:Encoding.UTF8) ){  
                string st=pBOARD.ConvertAll(P=>P.No).Connect("").Replace("-","+").Replace("0",".");
                fp.WriteLine(st);
            }
        }
    }  
}