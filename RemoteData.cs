﻿using api.pustalorc.xyz.JSON_Classes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace api.pustalorc.xyz
{
    public static class RemoteData
    {
        public static List<SimpleTeam> _simpleTeams = new List<SimpleTeam>();

        public static void RetrieveGroups()
        {
            var finalTeams = new List<SimpleTeam>();
            using (var web = new WebClient())
            {
                var teams = new List<Team>();

                foreach (var team in JsonConvert
                    .DeserializeObject<NuelTournament>(web.DownloadString(
                        "https://tournament-cms.dev.thenuel.com/rainbow-six-siege-university-league-winter-2019"))
                    .schedule.ToList().ConvertAll(k => k.tournamentId).Select(id => JsonConvert.DeserializeObject<Tournament>(
                        web.DownloadString($"https://teams.dev.thenuel.com/signup-pools/{id}"))).Where(team => team.teams.Any()))
                    teams.AddRange(team.teams.Where(k => k.members.Length >= 5).ToArray());

                foreach (var team in teams)
                {
                    var players = new List<SimplePlayer>();
                    var teamMMR = 0;

                    foreach (var player in team.members.ToList()
                        .ConvertAll(k => k.inGameName?.displayName ?? k.userId))
                    {
                        var playerData = JsonConvert.DeserializeObject<PlayerData>(
                            web.DownloadString($"https://r6tab.com/api/search.php?platform=uplay&search={player}"));

                        if ((playerData?.results?.Length ?? 0) > 0)
                        {
                            var data = playerData.results[0];
                            players.Add(new SimplePlayer
                            {
                                Name = player,
                                Rank =
                                    $"https://trackercdn.com/cdn/r6.tracker.network/ranks/svg/hd-rank{data.p_currentrank}.svg",
                                ProfilePicture =
                                    $"https://ubisoft-avatars.akamaized.net/{data.p_user}/default_146_146.png",
                                MMR = data.p_currentmmr
                            });
                            teamMMR += data.p_currentmmr;
                        }
                        else
                        {
                            players.Add(new SimplePlayer { Name = player, Rank = "", ProfilePicture = "", MMR = 0 });
                            teamMMR += 0;
                        }
                    }

                    var final = finalTeams.FirstOrDefault(k => k.Id.Equals(team.id));

                    if (final == null)
                        finalTeams.Add(new SimpleTeam { Id = team.id, Name = team.name, Members = players, TotalMMR = teamMMR, AverageMMR = teamMMR / players.Count });
                    else
                    {
                        final.Members = players;
                        final.TotalMMR = teamMMR;
                        final.AverageMMR = teamMMR / players.Count;
                    }
                }
            }

            _simpleTeams = finalTeams.OrderBy(k => k.Name).ToList();
        }
    }
}
