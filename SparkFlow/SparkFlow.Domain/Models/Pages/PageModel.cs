/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Models/Pages/PageModel.cs
 * Purpose: Core component: PageModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models.Pages
{
    public abstract class PageModel
    {
        public PagesEnum Page { get; set; }
    }
}