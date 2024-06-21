using System;
using System.Text.RegularExpressions;
using Downloader.Models;
using Downloader.ViewModels;
using Gherkin;
using Reqnroll;

namespace Euphonia.Tests.StepDefinitions
{
    [Binding]
    public class SongPathValidationStepDefinitions
    {
        string _currentSongName;
        private ScenarioContext _scenarioContext;

        public SongPathValidationStepDefinitions(ScenarioContext scenarioContext) 
        {
            _scenarioContext = scenarioContext;
        }

        [Given("The song file exists with a {string}")]
        public void GivenTheSongFileExistsWithA(string songName)
        {
            _scenarioContext["CurrentSongName"] = songName;
        }
        [When("I verify the song name")]
        public void WhenIVerifyTheSongName()
        {
            _currentSongName = Utils.CleanPath(_scenarioContext["CurrentSongName"].ToString());
        }

        [Then("The {string} should not contain any invalid characters")]
        public void ThenTheShouldNotContainAnyInvalidCharacters(string exp)
        {
            Assert.That(_currentSongName, Is.EqualTo(exp));
            Console.WriteLine($"Previous name was {_scenarioContext["CurrentSongName"]} and new name is {_currentSongName}");
        }
    }
}
