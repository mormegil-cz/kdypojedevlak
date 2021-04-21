using System.Collections.Generic;
using KdyPojedeVlak.Engine.Djr;

namespace KdyPojedeVlak.Models
{
    public static class DisplayConsts
    {
        public static readonly Dictionary<TrainOperation, string> TrainOperationIcons = new()
        {
            { TrainOperation.Unknown, "" },
            { TrainOperation.StopRequested, "" },
            { TrainOperation.Customs, "🛂" },
            { TrainOperation.Other, "" },
            { TrainOperation.EmbarkOnly, "◗" },
            { TrainOperation.DisembarkOnly, "◖" },
            { TrainOperation.RequestStop, "⨯" },
            { TrainOperation.DepartOnArrival, "△" },
            { TrainOperation.DepartAfterDisembark, "⊥" },
            { TrainOperation.NoWaitForConnections, "☉" },
            { TrainOperation.Preheating, "" },
            { TrainOperation.Passthrough, "" },
            { TrainOperation.ConnectedTrains, "" },
            { TrainOperation.TrainConnection, "" },
            // TODO: Better symbol
            { TrainOperation.StopsAfterOpening, "⨹" },
            { TrainOperation.ShortStop, "▲" },
            { TrainOperation.HandicappedEmbark, "" },
            { TrainOperation.HandicappedDisembark, "" },
            { TrainOperation.WaitForDelayedTrains, "●" },
            { TrainOperation.OperationalStopOnly, "+" },
            { TrainOperation.NonpublicStop, "⟠" },
        };

        public static readonly Dictionary<TrainOperation, string> TrainOperationDescriptions = new()
        {
            { TrainOperation.Unknown, "" },
            { TrainOperation.StopRequested, "Požadavek na zastavení" },
            { TrainOperation.Customs, "Celní a pasové odbavení" },
            { TrainOperation.Other, "Jiný důvod pobytu" },
            { TrainOperation.EmbarkOnly, "Zastavení jen pro nástup" },
            { TrainOperation.DisembarkOnly, "Zastavení jen pro výstup" },
            { TrainOperation.RequestStop, "Zastavení jen na znamení" },
            { TrainOperation.DepartOnArrival, "Odjezd v čase příjezdu" },
            { TrainOperation.DepartAfterDisembark, "Odjezd hned po výstupu" },
            { TrainOperation.NoWaitForConnections, "Nečeká na žádné přípoje" },
            { TrainOperation.Preheating, "Předtápění" },
            { TrainOperation.Passthrough, "Průjezd" },
            { TrainOperation.ConnectedTrains, "Jízda spojených vlaků" },
            { TrainOperation.TrainConnection, "Návaznost" },
            { TrainOperation.StopsAfterOpening, "Zastavuje od otevření zastávky" },
            { TrainOperation.ShortStop, "Pobyt kratší než 1/2 minuty" },
            { TrainOperation.HandicappedEmbark, "Nástup osoby se sníženou mobilitou" },
            { TrainOperation.HandicappedDisembark, "Výstup osoby se sníženou mobilitou" },
            { TrainOperation.WaitForDelayedTrains, "Čekání na zpožděné vlaky" },
            { TrainOperation.OperationalStopOnly, "Zastavení jen z dopravních důvodů" },
            { TrainOperation.NonpublicStop, "Nezveřejněné zastavení" },
        };

        public static readonly Dictionary<CentralPttNote, string> CentralPttNoteDescriptions = new()
        {
            { CentralPttNote.Unknown, "" },
            { CentralPttNote.Class12, "Ve vlaku řazeny k sezení i vozy 1.\u00A0vozové třídy" },
            { CentralPttNote.Class1, "Ve vlaku řazeny pouze vozy 1.\u00A0vozové třídy" },
            { CentralPttNote.Class2, "Ve vlaku řazeny pouze vozy 2.\u00A0vozové třídy" },
            { CentralPttNote.SleepingCar, "Lůžkový vůz (nerozlišuje se třída)" },
            { CentralPttNote.CouchetteCar, "Lehátkový vůz" },
            { CentralPttNote.DirectCar, "Přímý vůz" },
            { CentralPttNote.Cars, "Vůz pro přepravu osobních automobilů a motocyklů" },
            { CentralPttNote.Disabled, "Vůz vhodný pro přepravu cestujících na vozíku" },
            { CentralPttNote.Restaurant, "Restaurační vůz" },
            { CentralPttNote.Reservation, "Do označených vozů možno zakoupit místenku" },
            { CentralPttNote.ObligatoryReservation, "Povinná rezervace míst – nutno zakoupit místenku" },
            { CentralPttNote.Baggage, "Úschova během přepravy (do vyčerpání kapacity)" },
            { CentralPttNote.Bicycle, "Přeprava spoluzavazadel (do vyčerpání kapacity)" },
            { CentralPttNote.Transfer, "Nutno přestoupit" },
            { CentralPttNote.Refreshments, "Občerstvení (roznášková služba nebo samoobslužný automat)" },
            { CentralPttNote.Cafe, "Bistrovůz" },
            { CentralPttNote.BaggageReservation, "Úschova během přepravy s možností rezervace místa pro jízdní kolo" },
            { CentralPttNote.BaggageObligatoryReservation, "Úschova během přepravy s povinnou rezervací místa pro jízdní kolo" },
            { CentralPttNote.BicycleReservation, "Přeprava spoluzavazadel s možností rezervace místa pro jízdní kolo a cestujícího, v některých vlacích pouze pro jízdní kolo" },
            { CentralPttNote.BicycleObligatoryReservation, "Přeprava spoluzavazadel s povinnou rezervací místa pro jízdní kolo a cestujícího, v některých vlacích pouze pro jízdní kolo" },
            { CentralPttNote.PowerSocket, "Ve vlaku je řazen vůz s přípojkou 230\u00A0V" },
            { CentralPttNote.ReplacementBus, "ND – náhradní doprava" },
            { CentralPttNote.Children, "Vůz nebo oddíly vyhrazené pro cestující s dětmi do 10\u00A0let" },
            { CentralPttNote.DisabledPlatform, "Vůz vhodný pro přepravu cestujících na vozíku, vybavený zvedací plošinou" },
            { CentralPttNote.SelfService, "Samoobslužný způsob odbavení cestujících, cestující bez jízdenky nastupují do vlaku pouze dveřmi u stanoviště strojvedoucího" },
            { CentralPttNote.NoBicycles, "Přeprava jízdních kol jako spoluzavazadel vyloučena" },
            { CentralPttNote.HistoricTrain, "Historický vlak" },
            { CentralPttNote.WomenSectionCD, "Dámský oddíl (oddíl pro samostatně cestující ženy)" },
            { CentralPttNote.SilentSectionCD, "Tichý oddíl" },
            { CentralPttNote.WifiCD, "Ve vlaku je plánováno řazení vozu s bezdrátovým připojením k internetu" },
            { CentralPttNote.PortalCD, "Palubní portál" },
            { CentralPttNote.CinemaCD, "Dětské kino" },
            { CentralPttNote.IntegratedTransportSystem, "Vlak zařazen v integrovaném dopravním systému" },
            { CentralPttNote.DirectedBoarding, "Usměrněný nástup" },
        };

        public static readonly Dictionary<CentralPttNote, string> CentralPttNoteIcons = new()
        {
            { CentralPttNote.Unknown, "" },
            { CentralPttNote.Class12, "𝟣.𝟤." },
            { CentralPttNote.Class1, "𝟣." },
            { CentralPttNote.Class2, "𝟤." },
            { CentralPttNote.SleepingCar, "🛌\uFE0E" },
            { CentralPttNote.CouchetteCar, "⧦" },
            { CentralPttNote.DirectCar, "🚃\uFE0E" },
            { CentralPttNote.Cars, "🚗\uFE0E" },
            { CentralPttNote.Disabled, "♿\uFE0E" },
            { CentralPttNote.Restaurant, "🍴\uFE0E" },
            { CentralPttNote.Reservation, "𝗥" },
            { CentralPttNote.ObligatoryReservation, "R⃞" },
            { CentralPttNote.Baggage, "🛄\uFE0E" },
            { CentralPttNote.Bicycle, "🚲\uFE0E" },
            { CentralPttNote.Transfer, "◊" },
            { CentralPttNote.Refreshments, "🍸\uFE0E" },
            { CentralPttNote.Cafe, "☕\uFE0E" },
            { CentralPttNote.BaggageReservation, "🛄⃝" },
            { CentralPttNote.BaggageObligatoryReservation, "🛄" },
            { CentralPttNote.BicycleReservation, "🚲⃝" },
            { CentralPttNote.BicycleObligatoryReservation, "🚲⃞" },
            { CentralPttNote.PowerSocket, "⚇\uFE0E" },
            { CentralPttNote.ReplacementBus, "🚌\uFE0E" },
            { CentralPttNote.Children, "𝗗" },
            { CentralPttNote.DisabledPlatform, "♿⃞" },
            { CentralPttNote.SelfService, "👁\uFE0E" },
            { CentralPttNote.NoBicycles, "🚲̸" },
            { CentralPttNote.HistoricTrain, "🚂\uFE0E" },
            { CentralPttNote.WomenSectionCD, "👩\uFE0E" },
            { CentralPttNote.SilentSectionCD, "🤫\uFE0E" },
            { CentralPttNote.WifiCD, "𝗐𝗂𝖿𝗂" },
            { CentralPttNote.PortalCD, "⏵⃞" },
            { CentralPttNote.CinemaCD, "𝗸𝗶𝗻𝗼" },
            { CentralPttNote.IntegratedTransportSystem, "⇔" },
            { CentralPttNote.DirectedBoarding, "⛝" },
        };

        public static Dictionary<TrainCategory, string> TrainCategoryNames = new()
        {
            { TrainCategory.Unknown, "" },
            { TrainCategory.EuroCity, "EC" },
            { TrainCategory.Intercity, "IC" },
            { TrainCategory.Express, "Ex" },
            { TrainCategory.EuroNight, "EN" },
            { TrainCategory.Regional, "Os" },
            { TrainCategory.SuperCity, "SC" },
            { TrainCategory.Rapid, "Sp" },
            { TrainCategory.FastTrain, "R" },
            { TrainCategory.RailJet, "rj" },
            { TrainCategory.Rex, "Rx" },
            { TrainCategory.TrilexExpres, "TLX" },
            { TrainCategory.Trilex, "TL" },
            { TrainCategory.LeoExpres, "LE" },
            { TrainCategory.Regiojet, "RJ" },
            { TrainCategory.ArrivaExpress, "AEx" },
            { TrainCategory.NightJet, "NJ" },
            { TrainCategory.LeoExpresTenders, "LET" },
        };

        public static Dictionary<TrafficType, string> TrafficTypeNames = new()
        {
            { TrafficType.Unknown, "" },
            { TrafficType.Os, "Osobní vlak" },
            { TrafficType.Ex, "Expres" },
            { TrafficType.R, "Rychlík" },
            { TrafficType.Sp, "Spěšný vlak" },
            { TrafficType.Sv, "Soupravový vlak" },
            { TrafficType.Nex, "Nákladní expres" },
            { TrafficType.Pn, "Průběžný nákladní vlak" },
            { TrafficType.Mn, "Manipulační vlak" },
            { TrafficType.Lv, "Lokomotivní vlak" },
            { TrafficType.Vleč, "Vlečkový vlak" },
            { TrafficType.Služ, "Služební vlak" },
            { TrafficType.Pom, "Nutný pomocný vlak" },
        };

        public static Dictionary<SubsidiaryLocationType, string> SubsidiaryLocationTypeNames = new()
        {
            { SubsidiaryLocationType.Unknown, "" },
            { SubsidiaryLocationType.None, "" },
            { SubsidiaryLocationType.StationTrack, "kolej " }
        };
    }
}