﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BattleInfoPlugin.Models.Raw;
using BattleInfoPlugin.Models.Repositories;
using Grabacr07.KanColleWrapper;
using Livet;

namespace BattleInfoPlugin.Models
{
    public class BattleData : NotificationObject
    {
        //FIXME 敵の開幕雷撃&連合艦隊がまだ不明(とりあえず第二艦隊が受けるようにしてる)
        
        #region Name変更通知プロパティ
        private string _Name;

        public string Name
        {
            get
            { return this._Name; }
            set
            { 
                if (this._Name == value)
                    return;
                this._Name = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region UpdatedTime変更通知プロパティ
        private DateTimeOffset _UpdatedTime;

        public DateTimeOffset UpdatedTime
        {
            get
            { return this._UpdatedTime; }
            set
            { 
                if (this._UpdatedTime == value)
                    return;
                this._UpdatedTime = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region BattleSituation変更通知プロパティ

        private BattleSituation _BattleSituation;

        public BattleSituation BattleSituation
        {
            get
            { return this._BattleSituation; }
            set
            {
                if (this._BattleSituation == value)
                    return;
                this._BattleSituation = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region FirstFleet変更通知プロパティ
        private FleetData _FirstFleet;

        public FleetData FirstFleet
        {
            get
            { return this._FirstFleet; }
            set
            { 
                if (this._FirstFleet == value)
                    return;
                this._FirstFleet = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region SecondFleet変更通知プロパティ
        private FleetData _SecondFleet;

        public FleetData SecondFleet
        {
            get
            { return this._SecondFleet; }
            set
            { 
                if (this._SecondFleet == value)
                    return;
                this._SecondFleet = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region Enemies変更通知プロパティ
        private FleetData _Enemies;

        public FleetData Enemies
        {
            get
            { return this._Enemies; }
            set
            { 
                if (this._Enemies == value)
                    return;
                this._Enemies = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion


        #region FriendAirSupremacy変更通知プロパティ
        private AirSupremacy _FriendAirSupremacy = AirSupremacy.航空戦なし;

        public AirSupremacy FriendAirSupremacy
        {
            get
            { return this._FriendAirSupremacy; }
            set
            { 
                if (this._FriendAirSupremacy == value)
                    return;
                this._FriendAirSupremacy = value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        private int CurrentDeckId { get; set; }

        private readonly EnemyDataProvider provider = new EnemyDataProvider();

        public BattleData()
        {
            var proxy = KanColleClient.Current.Proxy;

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/battle")
                .TryParse<battle_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_battle_midnight/sp_midnight")
                .TryParse<battle_midnight_sp_midnight>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_combined_battle_airbattle
                .TryParse<combined_battle_airbattle>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_combined_battle_battle
                .TryParse<combined_battle_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/battle_water")
                .TryParse<combined_battle_battle_water>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/midnight_battle")
                .TryParse<combined_battle_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_combined_battle/sp_midnight")
                .TryParse<combined_battle_sp_midnight>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/battle")
                .TryParse<practice_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_practice/midnight_battle")
                .TryParse<practice_midnight_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_sortie/airbattle")
                .TryParse<sortie_airbattle>().Subscribe(x => this.Update(x.Data));

            proxy.api_req_sortie_battle
                .TryParse<sortie_battle>().Subscribe(x => this.Update(x.Data));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_map/start")
                .TryParse<map_start_next>().Subscribe(x => this.UpdateFleetsByStartNext(x.Data, x.Request["api_deck_id"]));

            proxy.ApiSessionSource.Where(x => x.PathAndQuery == "/kcsapi/api_req_map/next")
                .TryParse<map_start_next>().Subscribe(x => this.UpdateFleetsByStartNext(x.Data));

        }

        #region Update From Battle SvData

        public void Update(battle_midnight_battle data)
        {
            this.Name = "通常 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());
        }

        public void Update(battle_midnight_sp_midnight data)
        {
            this.Name = "通常 - 開幕夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.FriendAirSupremacy = AirSupremacy.航空戦なし;
        }

        public void Update(combined_battle_airbattle data)
        {
            this.Name = "連合艦隊 - 航空戦 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_kouku2.GetFirstFleetDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_kouku2.GetSecondFleetDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_kouku2.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy(); //航空戦2回目はスルー
        }

        public void Update(combined_battle_battle data)
        {
            this.Name = "連合艦隊 - 機動部隊 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_hougeki3.GetFriendDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_hougeki3.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();
        }

        public void Update(combined_battle_battle_water data)
        {
            this.Name = "連合艦隊 - 水上部隊 - 昼戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages()
                );

            this.SecondFleet.CalcDamages(
                data.api_kouku.GetSecondFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki3.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_hougeki3.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();
        }

        public void Update(combined_battle_midnight_battle data)
        {
            this.Name = "連合艦隊 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.SecondFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());
        }

        public void Update(combined_battle_sp_midnight data)
        {
            this.Name = "連合艦隊 - 開幕夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps, data.api_maxhps_combined);
            this.UpdateNowHP(data.api_nowhps, data.api_nowhps_combined);

            this.SecondFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());

            this.FriendAirSupremacy = AirSupremacy.航空戦なし;
        }

        public void Update(practice_battle data)
        {
            this.Name = "演習 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();
        }

        public void Update(practice_midnight_battle data)
        {
            this.Name = "演習 - 夜戦";

            this.UpdateFleets(data.api_deck_id, data.api_ship_ke);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(data.api_hougeki.GetFriendDamages());

            this.Enemies.CalcDamages(data.api_hougeki.GetEnemyDamages());
        }

        private void Update(sortie_airbattle data)
        {
            this.Name = "航空戦 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_kouku2.GetFirstFleetDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),    //将来的に増える可能性を想定して追加しておく
                data.api_kouku.GetEnemyDamages(),
                data.api_kouku2.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy(); // 航空戦2回目はスルー
        }

        private void Update(sortie_battle data)
        {
            this.Name = "通常 - 昼戦";

            this.UpdateFleets(data.api_dock_id, data.api_ship_ke, data.api_formation);
            this.UpdateMaxHP(data.api_maxhps);
            this.UpdateNowHP(data.api_nowhps);

            this.FirstFleet.CalcDamages(
                data.api_kouku.GetFirstFleetDamages(),
                data.api_opening_atack.GetFriendDamages(),
                data.api_hougeki1.GetFriendDamages(),
                data.api_hougeki2.GetFriendDamages(),
                data.api_raigeki.GetFriendDamages()
                );

            this.Enemies.CalcDamages(
                data.api_support_info.GetEnemyDamages(),
                data.api_kouku.GetEnemyDamages(),
                data.api_opening_atack.GetEnemyDamages(),
                data.api_hougeki1.GetEnemyDamages(),
                data.api_hougeki2.GetEnemyDamages(),
                data.api_raigeki.GetEnemyDamages()
                );

            this.FriendAirSupremacy = data.api_kouku.GetAirSupremacy();
        }

        #endregion

        private void UpdateFleetsByStartNext(map_start_next startNext, string api_deck_id = null)
        {
            this.UpdatedTime = DateTimeOffset.Now;
            this.Name = "次マス情報";

            this.BattleSituation = BattleSituation.なし;
            this.FriendAirSupremacy = AirSupremacy.航空戦なし;
            if (this.FirstFleet != null) this.FirstFleet.Formation = Formation.なし;
            this.Enemies = this.provider.GetNextEnemyFleet(startNext);

            if (api_deck_id != null) this.CurrentDeckId = int.Parse(api_deck_id);
            if (this.CurrentDeckId < 1) return;

            this.UpdateFriendFleets(this.CurrentDeckId);
        }

        private void UpdateFleets(
            int api_deck_id,
            int[] api_ship_ke,
            int[] api_formation = null)
        {
            this.UpdatedTime = DateTimeOffset.Now;
            this.UpdateFriendFleets(api_deck_id);

            var master = KanColleClient.Current.Master.Ships;
            this.Enemies = new FleetData(
                api_ship_ke.Where(x => x != -1).Select(x => new MastersShipData(master[x])).ToArray(),
                this.Enemies != null ? this.Enemies.Formation : Formation.なし,
                this.Enemies != null ? this.Enemies.Name : "");

            if (api_formation != null)
            {
                this.BattleSituation = (BattleSituation)api_formation[2];
                if (this.FirstFleet != null) this.FirstFleet.Formation = (Formation)api_formation[0];
                if (this.Enemies != null) this.Enemies.Formation = (Formation)api_formation[1];
            }

            this.CurrentDeckId = api_deck_id;
        }

        private void UpdateFriendFleets(int deckID)
        {
            var organization = KanColleClient.Current.Homeport.Organization;
            this.FirstFleet = new FleetData(
                organization.Fleets[deckID].Ships.Select(s => new MembersShipData(s)).ToArray(),
                this.FirstFleet != null ? this.FirstFleet.Formation : Formation.なし,
                organization.Fleets[deckID].Name);
            this.SecondFleet = new FleetData(
                organization.Combined && deckID == 1
                    ? organization.Fleets[2].Ships.Select(s => new MembersShipData(s)).ToArray()
                    : new MembersShipData[0],
                this.SecondFleet != null ? this.SecondFleet.Formation : Formation.なし,
                organization.Fleets[2].Name);
        }

        private void UpdateMaxHP(int[] api_maxhps, int[] api_maxhps_combined = null)
        {
            this.FirstFleet.Ships.SetValues(api_maxhps.GetFriendData(), (s, v) => s.MaxHP = v);
            this.Enemies.Ships.SetValues(api_maxhps.GetEnemyData(), (s, v) => s.MaxHP = v);

            if (api_maxhps_combined == null) return;
            this.SecondFleet.Ships.SetValues(api_maxhps_combined.GetFriendData(), (s, v) => s.MaxHP = v);
        }

        private void UpdateNowHP(int[] api_nowhps, int[] api_nowhps_combined = null)
        {
            this.FirstFleet.Ships.SetValues(api_nowhps.GetFriendData(), (s, v) => s.NowHP = v);
            this.Enemies.Ships.SetValues(api_nowhps.GetEnemyData(), (s, v) => s.NowHP = v);

            if (api_nowhps_combined == null) return;
            this.SecondFleet.Ships.SetValues(api_nowhps_combined.GetFriendData(), (s, v) => s.NowHP = v);
        }
    }
}
