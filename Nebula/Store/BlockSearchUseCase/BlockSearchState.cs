using Lyra.Core.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nebula.Store.BlockSearchUseCase
{
	public class BlockSearchState
	{
		public bool IsLoading { get; }
		public Block block { get; }
		public Block prevBlock { get; }

		public string Key { get; }
		public long MaxHeight { get; }

		public long prevHeight => block.Height > 1 ? block.Height - 1 : block.Height;
		public long nextHeight => block.Height < MaxHeight ? block.Height + 1 : block.Height;
		public bool IsBlockValid => block.Hash.Equals(block.CalculateHash());
		public string Error { get; }

		public BlockSearchState(bool isLoading, Block blockResult, Block previousBlock, string pageKey, long maxHeight, string errmsg)
		{
			IsLoading = isLoading;
			block = blockResult ?? null;
			prevBlock = previousBlock;
			Key = pageKey;
			MaxHeight = maxHeight;
			Error = errmsg;
		}

		public List<string> Paging()
        {
			List<string> strs = new List<string>();
			int dot = 0;
			for(int i = 1; i <= MaxHeight; i++)
            {
				if(i == block.Height)
				{
					if(dot > 0)
                    {
						strs.Add("<span>...</span>");
                    }
					strs.Add($"<a class=\"active\" href=\"/showblock/{Key}/{i}\">{i}</a>");
					dot = 0;
				}
                else if (i < 3 || (i > block.Height - 3 && i < block.Height) || (i > block.Height && i < block.Height + 3) || i > MaxHeight - 2)
				{
					if (dot > 0)
					{
						strs.Add("<span>...</span>");
					}
					strs.Add($"<a href=\"/showblock/{Key}/{i}\">{i}</a>");
					dot = 0;
				}
                else
                {
					dot++;
                }
            }
			return strs;
        }

		public string FancyShow()
        {
			var r = new Regex(@"BlockType: \w+");
			var html = r.Replace(block.Print(), Matcher);

			html = Regex.Replace(html, @"\s(\w{43,})\W", HashMatcher);

			// display send amount or receive amount

			if(block is SendTransferBlock sendBlock && prevBlock != null)
            {
				var delta = sendBlock.GetBalanceChanges(prevBlock as TransactionBlock);

				foreach(var chg in delta.Changes)
					html += $"\nSending {chg.Value} {chg.Key}";
				html += $"\nFee: {delta.FeeAmount} {delta.FeeCode}";
			}
			else if (block is ReceiveTransferBlock recvBlock && prevBlock != null)
			{
				var delta = recvBlock.GetBalanceChanges(prevBlock as TransactionBlock);

				foreach (var chg in delta.Changes)
					html += $"\nReceiving {chg.Value} {chg.Key}";
				html += $"\nFee: {delta.FeeAmount} {delta.FeeCode}";
			}

			return html;
        }

		private string HashMatcher(Match m)
        {
			var all = m.Groups[0].Value;
			var hash = m.Groups[1].Value;

			if (hash == block.Hash)
				return all;
			if (hash.Length == 43 || hash.Length == 44 || (hash.Length > 90 && hash.StartsWith('L')))
				return all.Replace(hash, $"<a href='/showblock/{hash}'>{hash}</a>");
			else
				return all;
		}

		private string Matcher(Match m)
        {
			return $"<b style='color: blue'>{m.Groups.Cast<Group>().FirstOrDefault()}</b>";
		}
	}
}
