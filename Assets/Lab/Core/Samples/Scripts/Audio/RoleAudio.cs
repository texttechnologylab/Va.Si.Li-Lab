using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaSiLi.SceneManagement;

public class RoleAudio : MonoBehaviour
{
    public AudioSource RoleSound;
    public AudioClip[] audioSources;
    private Dictionary<string, Dictionary<string, int>> roleAudioIndex;
    void Start()
    {
        roleAudioIndex = new Dictionary<string, Dictionary<string, int>>()
        {
             { "OrganisationDistribution", new Dictionary<string, int>()
                {
                     { "Arbeitnehmer*in A (AA)", 0},
                     { "Arbeitnehmer*in B (AB)", 1 },
                     { "Arbeitnehmer*in C (AC)", 3 },
                     { "Geschäftsführer*in (B)", 2 },
                     { "Beobachter*innen (BE)", -1}
                }
             },
             { "OrganisationEducation", new Dictionary<string, int>()
                {
                     { "Arbeitnehmer*in A (AA)", 12},
                     { "Arbeitnehmer*in B (AB)", 13},
                     { "Supervisor*in (S)", 15},
                     { "Geschäftsführer*in (B)", 14},
                     { "Beobachter*innen (BE)", -1}
                }
             },
             { "SchoolDistribution", new Dictionary<string, int>()
                {
                     { "Lehrer*in (B)", 4},
                     { "Schüler*in A (SA)", 5},
                     { "Schüler*in B (SB)", 6 },
                     { "Schüler*in C (SC)", 7 },
                }
             },
             { "SchoolHetero", new Dictionary<string, int>()
                {
                     { "Lehrkraft (A)", 16},
                     { "Schüler*in A (SA)", 17},
                     { "Schüler*in B (SB)", 18},
                     { "Schüler*in C (SC)", 19},
                     { "Beobachter*innen (C)", -1}
                }
             },
             { "SocialDistribution", new Dictionary<string, int>()
                {
                     {"Klient*in A (KA)", 8},
                     {"Klient*in B (KB)", 9 },
                     {"Klient*in C (KC)", 10 },
                     {"Sozialarbeiter*in (B)", 11},
                     {"Andere Teilnehmer*innen (A)", -1}
                }
             },
              { "SocialAmbiguity", new Dictionary<string, int>()
                {
                     {"Klient*in A (KA)", 20},
                     {"Klient*in B (KB)", 21},
                     {"Klient*in C (KC)", 22},
                     {"Klient*in D (KD)", 23},
                     {"Berufsberater*in (B)", 24},
                }
             },
        };
    }


    public void PlayRoleAudio()
    {
        string szenario = SceneManager.CurrentScene?.internalName;
        string roleName = RoleManager.CurrentRole?.id;
        if (roleAudioIndex.ContainsKey(szenario) && roleAudioIndex[szenario].ContainsKey(roleName))
        {

            int roleID = roleAudioIndex[szenario][roleName];
            if (roleID != -1)
            {
                RoleSound.clip = audioSources[roleID];
                if (!RoleSound.isPlaying)
                {
                    RoleSound.Play();
                }
                else
                {
                    RoleSound.Stop();
                }

            }

        }
    }
}
