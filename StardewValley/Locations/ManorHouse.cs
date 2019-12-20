using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using xTile.Dimensions;

namespace StardewValley.Locations
{
	public class ManorHouse : GameLocation
	{
		[XmlIgnore]
		private Dictionary<string, Farmer> sendMoneyMapping = new Dictionary<string, Farmer>();

		private static readonly bool changeWalletTypeImmediately;

		public ManorHouse()
		{
		}

		public ManorHouse(string mapPath, string name)
			: base(mapPath, name)
		{
		}

		public override bool performAction(string action, Farmer who, Location tileLocation)
		{
			if (action != null && who.IsLocalPlayer)
			{
				switch (action.Split(' ')[0])
				{
				case "MayorFridge":
				{
					int numberFound = 0;
					for (int j = 0; j < who.items.Count; j++)
					{
						if (who.items[j] != null && (int)who.items[j].parentSheetIndex == 284)
						{
							numberFound += who.items[j].Stack;
						}
					}
					if (numberFound >= 10 && !who.hasOrWillReceiveMail("TH_MayorFridge") && who.hasOrWillReceiveMail("TH_Railroad"))
					{
						int numRemoved = 0;
						for (int i = 0; i < who.items.Count; i++)
						{
							if (who.items[i] == null || (int)who.items[i].parentSheetIndex != 284)
							{
								continue;
							}
							while (numRemoved < 10)
							{
								who.items[i].Stack--;
								numRemoved++;
								if (who.items[i].Stack == 0)
								{
									who.items[i] = null;
									break;
								}
							}
							if (numRemoved >= 10)
							{
								break;
							}
						}
						Game1.player.CanMove = false;
						localSound("coin");
						Game1.player.mailReceived.Add("TH_MayorFridge");
						Game1.multipleDialogues(new string[2]
						{
							Game1.content.LoadString("Strings\\Locations:ManorHouse_MayorFridge_ConsumeBeets"),
							Game1.content.LoadString("Strings\\Locations:ManorHouse_MayorFridge_MrQiNote")
						});
						Game1.player.removeQuest(3);
						Game1.player.addQuest(4);
					}
					else if (who.hasOrWillReceiveMail("TH_MayorFridge"))
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_MayorFridge_MrQiNote"));
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_MayorFridge_Initial"));
					}
					break;
				}
				case "DivorceBook":
					if ((bool)Game1.player.divorceTonight)
					{
						string s2 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_CancelQuestion_" + Game1.player.spouse);
						if (s2 == null)
						{
							s2 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_CancelQuestion");
						}
						createQuestionDialogue(s2, createYesNoResponses(), "divorceCancel");
					}
					else if (Game1.player.isMarried())
					{
						string s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question_" + Game1.player.spouse);
						if (s == null)
						{
							s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question");
						}
						createQuestionDialogue(s, createYesNoResponses(), "divorce");
					}
					else
					{
						Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_NoSpouse"));
					}
					break;
				case "LedgerBook":
					readLedgerBook();
					break;
				}
			}
			return base.performAction(action, who, tileLocation);
		}

		private void readLedgerBook()
		{
			if (Game1.player.useSeparateWallets)
			{
				if (Game1.IsMasterGame)
				{
					List<Response> choices = new List<Response>();
					choices.Add(new Response("SendMoney", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SendMoney")));
					if ((bool)Game1.player.changeWalletTypeTonight)
					{
						choices.Add(new Response("CancelMerge", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_CancelMerge")));
					}
					else
					{
						choices.Add(new Response("MergeWallets", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_MergeWallets")));
					}
					choices.Add(new Response("Leave", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_Leave")));
					createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_HostQuestion"), choices.ToArray(), "ledgerOptions");
				}
				else
				{
					ChooseRecipient();
				}
			}
			else if (Game1.getAllFarmhands().Count() == 0)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_Singleplayer"));
			}
			else if (Game1.IsMasterGame)
			{
				if ((bool)Game1.player.changeWalletTypeTonight)
				{
					string s2 = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_CancelQuestion");
					createQuestionDialogue(s2, createYesNoResponses(), "cancelSeparateWallets");
				}
				else
				{
					string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_SeparateQuestion");
					createQuestionDialogue(s, createYesNoResponses(), "separateWallets");
				}
			}
			else
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SharedWallets_Client"));
			}
		}

		public void ChooseRecipient()
		{
			sendMoneyMapping.Clear();
			List<Response> otherFarmers = new List<Response>();
			foreach (Farmer farmer in Game1.getAllFarmers())
			{
				if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID && !farmer.isUnclaimedFarmhand)
				{
					string key = "Transfer" + (otherFarmers.Count + 1);
					string farmerName = farmer.Name;
					if (farmer.Name == "")
					{
						farmerName = Game1.content.LoadString("Strings\\UI:Chat_PlayerJoinedNewName");
					}
					otherFarmers.Add(new Response(key, farmerName));
					sendMoneyMapping.Add(key, farmer);
				}
			}
			if (otherFarmers.Count == 0)
			{
				Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_NoFarmhands"));
				return;
			}
			otherFarmers.Sort((Response x, Response y) => string.Compare(x.responseKey, y.responseKey));
			otherFarmers.Add(new Response("Cancel", Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_TransferCancel")));
			Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_TransferQuestion"), otherFarmers.ToArray(), "chooseRecipient");
		}

		private void beginSendMoney(Farmer recipient)
		{
			Game1.activeClickableMenu = new DigitEntryMenu(Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_HowMuchQuestion"), delegate(int currentValue, int price, Farmer who)
			{
				sendMoney(recipient, currentValue);
			}, -1, 1, Game1.player.Money);
		}

		public void sendMoney(Farmer recipient, int amount)
		{
			Game1.playSound("smallSelect");
			Game1.player.Money -= amount;
			Game1.player.team.AddIndividualMoney(recipient, amount);
			Game1.player.stats.onMoneyGifted((uint)amount);
			if (amount == 1)
			{
				Game1.multiplayer.globalChatInfoMessage("Sent1g", Game1.player.Name, recipient.Name);
			}
			else
			{
				Game1.multiplayer.globalChatInfoMessage("SentMoney", Game1.player.Name, recipient.Name, Utility.getNumberWithCommas(amount));
			}
			Game1.exitActiveMenu();
		}

		public static void SeparateWallets()
		{
			if (!Game1.player.useSeparateWallets && Game1.IsMasterGame)
			{
				Game1.player.changeWalletTypeTonight.Value = false;
				int totalMoney = Game1.player.Money;
				int farmerCount = 0;
				foreach (Farmer allFarmer in Game1.getAllFarmers())
				{
					if (!allFarmer.isUnclaimedFarmhand)
					{
						farmerCount++;
					}
				}
				int splitMoney = totalMoney / Math.Max(farmerCount, 1);
				Game1.player.team.useSeparateWallets.Value = true;
				foreach (Farmer farmer in Game1.getAllFarmers())
				{
					if (!farmer.isUnclaimedFarmhand)
					{
						Game1.player.team.SetIndividualMoney(farmer, splitMoney);
					}
				}
				Game1.multiplayer.globalChatInfoMessage("SeparatedWallets", Game1.player.Name, splitMoney.ToString());
			}
		}

		public static void MergeWallets()
		{
			if (Game1.player.useSeparateWallets && Game1.IsMasterGame)
			{
				Game1.player.changeWalletTypeTonight.Value = false;
				int totalMoney = 0;
				foreach (Farmer farmer in Game1.getAllFarmers())
				{
					if (!farmer.isUnclaimedFarmhand)
					{
						totalMoney += Game1.player.team.GetIndividualMoney(farmer);
					}
				}
				Game1.player.team.useSeparateWallets.Value = false;
				Game1.player.team.money.Value = totalMoney;
				Game1.multiplayer.globalChatInfoMessage("MergedWallets", Game1.player.Name);
			}
		}

		public override bool answerDialogueAction(string questionAndAnswer, string[] questionParams)
		{
			string s5 = null;
			if (questionAndAnswer == null)
			{
				return false;
			}
			switch (questionAndAnswer)
			{
			case "divorceCancel_Yes":
				if ((bool)Game1.player.divorceTonight)
				{
					Game1.player.divorceTonight.Value = false;
					if (!Game1.player.isRoommate(Game1.player.spouse))
					{
						Game1.player.addUnearnedMoney(50000);
					}
					s5 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Cancelled_" + Game1.player.spouse);
					if (s5 == null)
					{
						s5 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Cancelled");
					}
					Game1.drawObjectDialogue(s5);
					if (!Game1.player.isRoommate(Game1.player.spouse))
					{
						Game1.multiplayer.globalChatInfoMessage("DivorceCancel", Game1.player.Name);
					}
				}
				break;
			case "divorce_Yes":
				if (Game1.player.Money >= 50000 || Game1.player.spouse == "Krobus")
				{
					if (!Game1.player.isRoommate(Game1.player.spouse))
					{
						Game1.player.Money -= 50000;
					}
					Game1.player.divorceTonight.Value = true;
					s5 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Filed_" + Game1.player.spouse);
					if (s5 == null)
					{
						s5 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Filed");
					}
					Game1.drawObjectDialogue(s5);
					if (!Game1.player.isRoommate(Game1.player.spouse))
					{
						Game1.multiplayer.globalChatInfoMessage("Divorce", Game1.player.Name);
					}
				}
				else
				{
					Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
				}
				break;
			case "separateWallets_Yes":
				if (changeWalletTypeImmediately)
				{
					SeparateWallets();
					break;
				}
				Game1.player.changeWalletTypeTonight.Value = true;
				Game1.multiplayer.globalChatInfoMessage("SeparateWallets", Game1.player.Name);
				break;
			case "cancelSeparateWallets_Yes":
				Game1.player.changeWalletTypeTonight.Value = false;
				Game1.multiplayer.globalChatInfoMessage("SeparateWalletsCancel", Game1.player.Name);
				break;
			case "mergeWallets_Yes":
				if (changeWalletTypeImmediately)
				{
					MergeWallets();
					break;
				}
				Game1.player.changeWalletTypeTonight.Value = true;
				Game1.multiplayer.globalChatInfoMessage("MergeWallets", Game1.player.Name);
				break;
			case "cancelMergeWallets_Yes":
				Game1.player.changeWalletTypeTonight.Value = false;
				Game1.multiplayer.globalChatInfoMessage("MergeWalletsCancel", Game1.player.Name);
				break;
			case "ledgerOptions_SendMoney":
				ChooseRecipient();
				break;
			case "ledgerOptions_MergeWallets":
				s5 = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_MergeQuestion");
				createQuestionDialogue(s5, createYesNoResponses(), "mergeWallets");
				break;
			case "ledgerOptions_CancelMerge":
				s5 = Game1.content.LoadString("Strings\\Locations:ManorHouse_LedgerBook_SeparateWallets_CancelQuestion");
				createQuestionDialogue(s5, createYesNoResponses(), "cancelMergeWallets");
				break;
			}
			if (questionAndAnswer.Contains("Transfer"))
			{
				string answer = questionAndAnswer.Split('_')[1];
				beginSendMoney(sendMoneyMapping[answer]);
				sendMoneyMapping.Clear();
			}
			return base.answerDialogueAction(questionAndAnswer, questionParams);
		}
	}
}
