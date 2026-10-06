using POS_BIGA.Services;
using System;

namespace POS_BIGA.Services
{
    /// <summary>
    /// Extension methods لـ AppStateService
    /// </summary>
    public static class AppStateServiceExtensions
    {
        public static void RaiseInventoriesChanged(this IAppStateService appState)
        {
            appState.NotifyInventoriesChanged();
        }

        public static void RaiseCategoriesChanged(this IAppStateService appState)
        {
            appState.NotifyCategoriesChanged();
        }

        public static void RaiseUnitsChanged(this IAppStateService appState)
        {
            appState.NotifyUnitsChanged();
        }

        public static void RaiseProductsChanged(this IAppStateService appState)
        {
            appState.NotifyProductsChanged();
        }

        public static void RaiseCustomersChanged(this IAppStateService appState)
        {
            appState.NotifyCustomersChanged();
        }

        public static void RaiseCreditsChanged(this IAppStateService appState)
        {
            appState.NotifyCreditsChanged();
        }
    }
}
