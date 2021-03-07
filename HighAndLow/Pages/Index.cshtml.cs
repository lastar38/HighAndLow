using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace HighAndLow.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public bool ParrentPlayer { get; set; } = false;


        public void OnGet()
        {
            StartPlayer();
            DealCards();
            NextCardShow();
        }

        public IActionResult OnPostGameStart()
        {
            StartPlayer();
            DealCards();
            NextCardShow();

            return Page();
        }

        // 最初の親を決める
        private void StartPlayer()
        {
            if(RandomNumberGenerator.GetInt32(0, 2) == 1)
            {
                ParrentPlayer = true;
            }
        }

        // カードを配る
        private void DealCards()
        {

        }

        // 次のカードを配る
        private void NextCardShow()
        {

        }
    }
}
