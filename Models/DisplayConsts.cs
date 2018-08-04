using System;
using System.Collections.Generic;
using KdyPojedeVlak.Engine.Djr;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KdyPojedeVlak.Models
{
    public static class DisplayConsts
    {
        public static readonly Dictionary<TrainOperation, String> TrainOperationIcons = new Dictionary<TrainOperation, string>
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

        public static readonly Dictionary<TrainOperation, String> TrainOperationDescriptions = new Dictionary<TrainOperation, string>
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
    }
}