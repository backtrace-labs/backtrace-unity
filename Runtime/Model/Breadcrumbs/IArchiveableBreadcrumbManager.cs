namespace Backtrace.Unity.Model.Breadcrumbs
{
    internal interface IArchiveableBreadcrumbManager
    {
        /// <summary>
        /// Archive current breadcrumb file
        /// </summary>
        /// <returns>Path to the archived breadcurmb file if archiving process went successfully. Otherwise empty string.</returns>
        string Archive();
    }
}
