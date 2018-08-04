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
            { TrainOperation.Customs, "" },
            { TrainOperation.Other, "" },
            { TrainOperation.EmbarkOnly, "◗" },
            { TrainOperation.DisembarkOnly, "◖" },
            { TrainOperation.RequestStop, "⨯" },
            { TrainOperation.DepartOnArrival, "⊥" },
            { TrainOperation.DepartAfterDisembark, "" },
            { TrainOperation.NoWaitForConnections, "☉" },
            { TrainOperation.Preheating, "" },
            { TrainOperation.Passthrough, "" },
            { TrainOperation.ConnectedTrains, "" },
            { TrainOperation.TrainConnection, "" },
            // TODO: Better symbol
            { TrainOperation.StopsAfterOpening, "⨹" },
            { TrainOperation.ShortStop, "" },
            { TrainOperation.HandicappedEmbark, "" },
            { TrainOperation.HandicappedDisembark, "" },
            { TrainOperation.WaitForDelayedTrains, "●" },
            { TrainOperation.OperationalStopOnly, "" },
            { TrainOperation.NonpublicStop, "" },
        };
    }
}