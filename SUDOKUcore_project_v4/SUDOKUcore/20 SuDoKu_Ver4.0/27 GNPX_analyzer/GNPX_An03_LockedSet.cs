using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;
using GIDOO_space;

namespace GNPXcore{
    public class LockedSetGen: AnalyzerBaseV2{
        private UInt128 cells128;

        public LockedSetGen( GNPX_AnalyzerMan AnMan ): base(AnMan){
            UInt128 cells128=0;
            pBOARD.ForEach( p => { if(p.No!=0) cells128 |= (UInt128)1<<p.rc; } );        
        }

        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page33.html


        public bool LockedSet2() => LockedSet_Master(2);  //2D
        public bool LockedSet3() => LockedSet_Master(3);  //3D
        public bool LockedSet4() => LockedSet_Master(4);  //4D
        public bool LockedSet5() => LockedSet_Master(5);  //complementary to 4D Hidden
        public bool LockedSet6() => LockedSet_Master(6);  //complementary to 3D Hidden
        public bool LockedSet7() => LockedSet_Master(7);  //complementary to 2D Hidden

        public bool LockedSet2Hidden() => LockedSet_Master(2,HiddenFlag:true); //2D Hidden
        public bool LockedSet3Hidden() => LockedSet_Master(3,HiddenFlag:true);  //3D Hidden
        public bool LockedSet4Hidden() => LockedSet_Master(4,HiddenFlag:true);  //4D Hidden
        public bool LockedSet5Hidden() => LockedSet_Master(5,HiddenFlag:true);  //complementary to 4D 
        public bool LockedSet6Hidden() => LockedSet_Master(6,HiddenFlag:true);  //complementary to 3D 
        public bool LockedSet7Hidden() => LockedSet_Master(7,HiddenFlag:true);  //complementary to 2D 



        //   Extremely elegant algorithm !!!

        public bool LockedSet_Master( int sz, bool HiddenFlag=false ){
            UInt128 cell128 = pBOARD.Get_rc_BitExpression( no:-1 );
                        //WriteLine( $"               cell128:{cell128.ToString81()}" );

            for( int h=0; h<27; h++ ){
                UInt128 cells_in_house = cell128 & HouseCells128[h];    // cells_in_house : unresolved cells in the house
                        //WriteLine( $" cells_in_house h:{h}  {cells_in_house.ToString81()}" );

                int nc = cells_in_house.BitCount();
                if( nc <= sz ) continue;

                List<UCell>  cells = cells_in_house.IEGet_Cells(pBOARD).ToList();
                
                Combination cmbG = new Combination(nc,sz);
                while( cmbG.Successor() ){
                    int cmbG_index_bit = cmbG.ToBitExpression();

                    int noB_selected=0, noB_unselected=0, selBlk=0;    
                    UInt128 cellA = 0;
                    foreach( var (P,selectedF) in cmbG_index_bit.IEGet_cell_withFlag(cells:cells) ){
                        if(selectedF){ noB_selected |= P.FreeB; selBlk |= 1<<P.b; cellA |= (UInt128)1<<P.rc; }
                        else           noB_unselected |= P.FreeB;
                    }                        

                    if( (noB_selected&noB_unselected) == 0 ) continue;                  //any digits that can be excluded?

                    //=============== Naked Locked Set ===============
                    if( !HiddenFlag ){
                        if( noB_selected.BitCount() == sz ){                            //Number of selected cell's digits is sz
                            
                            // ----- solution found -----
                            if( h<18 && selBlk.BitCount()==1 ){ 
                                // When searching for a row or column and it is one block,
                                // there may be elements within the block that can be excluded.
                                int h2 = selBlk.BitToNum()+18;              //bit expression -> House_No(18-26)
                                UInt128 cellB = (cell128 & HouseCells128[h2]) - cellA;  // -: difference set. bit operation. not subtraction.

                                foreach( var P in cellB.IEGet_Cells(pBOARD) ){ P.CancelB = P.FreeB&noB_selected; };
                            }

                            string resST = "";
                            foreach( var (P,selectedF) in cmbG_index_bit.IEGet_cell_withFlag(cells:cells) ){
                                if( selectedF ){
                                    P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                    resST += " "+P.rc.ToRCString();
                                }
                                else P.CancelB = P.FreeB&noB_selected;
                            }
                            resST = resST.ToString_SameHouseComp()+" #"+noB_selected.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);

                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }

                    //=============== Hidden Locked Set ===============
                    else{       //if( HiddenFlag ){
                        if( noB_unselected.BitCount() == (nc-sz) ){                    //Number of unselected cell's digits is (nc-sz)

                             // ----- solution found -----
                            string resST="";
                            foreach( var (P,selectedF) in cmbG_index_bit.IEGet_cell_withFlag(cells:cells) ){
                                if( !selectedF )  continue;
                                P.CancelB = P.FreeB&noB_unselected;
                                P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                resST += " "+P.rc.ToRCString();
                            }
                            int nobR = noB_selected.DifSet(noB_unselected);
                            resST = resST.ToString_SameHouseComp()+" #"+nobR.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);

                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }
                }
            }
            return false;
        }

        private void _LockedSetResult( int sz, string resST, bool HiddenFlag ){
            string[]  lockedSet_type = {"Pair[2D]", "Triple[3D]", "Quartet[4D]", "Set[5D]", "Set[6D]", "Set{7D}" };

            SolCode = 2;
            string SolMsg = "Locked" + lockedSet_type[sz-2];
            if( HiddenFlag ) SolMsg += " hidden";
            SolMsg += " "+resST;
            Result = SolMsg;
            ResultLong = SolMsg;
        }

#if false

        public bool LockedSet_Master_old( int sz, bool HiddenFlag=false ){
            string resST="";
            UCell uc;
            for( int h=0; h<27; h++ ){
                List<UCell>  cells_in_house = pBOARD.IEGetCellInHouse(h,0x1FF).ToList();    //cells_in_house:List of unresolved cells in the house
                int nc = cells_in_house.Count;
                if( nc <= sz ) continue;
                
                Combination cmbG = new Combination(nc,sz);
                while( cmbG.Successor() ){
                    cells_in_house.ForEach(p => p.Selected=false);        // Initialize the cells in the cells_in_house.
                    int selBlk=0;
                    Array.ForEach(cmbG.Index, px=> { (uc=cells_in_house[px]).Selected=true; selBlk|=1<<uc.b; } );  //select cells by Combination

                    int noB_selected=0, noB_unselected=0;
                    cells_in_house.ForEach(P => {
                        if(P.Selected) noB_selected |= P.FreeB;
                        else           noB_unselected |= P.FreeB;
                    } ); 
                         // int noB_selected = cells_in_house.Where(p=> p.Selected).Aggregate(0,(Q,q)=>Q|=q.FreeB);
                         // int noB_unselected = cells_in_house.Where(p=>!p.Selected).Aggregate(0,(Q,q)=>Q|=q.FreeB);


                    if( (noB_selected&noB_unselected) == 0 ) continue;                      //any digits that can be excluded?

                    //=============== Naked Locked Set ===============
                    if( !HiddenFlag ){
                        if( noB_selected.BitCount()==sz ){                          //Number of selected cell's digits is sz
                            if( h<18 && selBlk.BitCount()==1 ){ 
                                // When searching for a row or column and it is one block,
                                // there may be elements within the block that can be excluded.
                                int tfx2 = selBlk.BitToNum()+18;              //bit expression -> House_No(18-26)
                                List<UCell> PLst = pBOARD.IEGetCellInHouse(tfx2,noB_selected).ToList();
                                cells_in_house.ForEach(P=> {if(P.Selected) PLst.Remove(P); });
                                if(PLst.Count>0)  PLst.ForEach(P=> { P.CancelB = P.FreeB&noB_selected; } );
                            }

                            resST = "";
                            foreach( var P in cells_in_house ){
                                if( P.Selected ){
                                    P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                    resST += " "+P.rc.ToRCString();
                                }
                                else P.CancelB = P.FreeB&noB_selected;
                            }
                            resST = resST.ToString_SameHouseComp()+" #"+noB_selected.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }

                    //=============== Hidden Locked Set ===============
                    if( HiddenFlag ){
                        if( noB_unselected.BitCount()==(nc-sz) ){                    //Number of unselected cell's digits is (nc-sz)
                            resST="";
                            foreach( var P in cells_in_house.Where(p=>p.Selected) ){
                                P.CancelB = P.FreeB&noB_unselected;
                                P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                resST += " "+P.rc.ToRCString();
                            }
                            int nobR = noB_selected.DifSet(noB_unselected);
                            resST = resST.ToString_SameHouseComp()+" #"+nobR.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }
                }
            }
            return false;
        }


        public bool LockedSetSub_old_old( int sz, bool HiddenFlag=false ){
            string resST="";
            UCell uc;
            for( int h=0; h<27; h++ ){
                List<UCell>  cells_in_house = pBOARD.IEGetCellInHouse(h,0x1FF).ToList();    //selecte cells in house
                int nc = cells_in_house.Count;
                if(nc<=sz) continue;
                
                Combination cmbG = new Combination(nc,sz);
                while( cmbG.Successor() ){
                    cells_in_house.ForEach(p=>p.Selected=false);        //(Initialization of cells in the house. The target is several.)
                    int selBlk=0;
                    Array.ForEach(cmbG.Index, p=> { (uc=cells_in_house[p]).Selected=true; selBlk|=1<<uc.b; });  //selecte cells by Combination

                    int noB_selected=0, noB_unselected=0;
                    cells_in_house.ForEach(p=>{
                        if(p.Selected) noB_selected |= p.FreeB;
                        else           noB_unselected |= p.FreeB;
                    } ); 
                    if( (noB_selected&noB_unselected)==0 ) continue;                              //any digits that can be excluded?

                    //=============== Naked Locked Set ===============
                    if( !HiddenFlag ){
                        if( noB_selected.BitCount()==sz ){                                  //Number of selected cell's digits is sz
                            if( h<18 && selBlk.BitCount()==1 ){
                                int tfx2=selBlk.BitToNum()+18;                      //bit expression -> House_No(18-26)
                                List<UCell> PLst = pBOARD.IEGetCellInHouse(tfx2,noB_selected).ToList();
                                cells_in_house.ForEach(P=> {if(P.Selected) PLst.Remove(P); });
                                if( PLst.Count>0 ) PLst.ForEach(P=> { P.CancelB=P.FreeB&noB_selected; } );
                            }

                            resST="";
                            foreach( var P in cells_in_house ){
                                if( P.Selected ){
                                    P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                    resST += " "+P.rc.ToRCString();
                                }
                                else P.CancelB=P.FreeB&noB_selected;
                            }
                            resST = resST.ToString_SameHouseComp()+" #"+noB_selected.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }

                    //=============== Hidden Locked Set ===============
                    if( HiddenFlag ){
                        if( noB_unselected.BitCount()==(nc-sz) ){                          //Number of unselected cell's digits is (nc-sz)
                            resST="";
                            foreach( var P in cells_in_house.Where(p=>p.Selected) ){
                                P.CancelB = P.FreeB&noB_unselected;
                                P.Set_CellColorBkgColor_noBit(noB_selected,AttCr,SolBkCr);
                                resST += " "+P.rc.ToRCString();
                            }
                            int nobR = noB_selected.DifSet(noB_unselected);
                            resST = resST.ToString_SameHouseComp()+" #"+nobR.ToBitStringN(9);
                            _LockedSetResult(sz,resST,HiddenFlag);
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) ) return true;
                        }
                    }
                }
            }
            return false;
        }
#endif
    }
}