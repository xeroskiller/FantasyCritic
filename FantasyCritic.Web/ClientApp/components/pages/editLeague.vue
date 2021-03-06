<template>
  <div>
    <div v-if="selectedLeagueOptions && possibleLeagueOptions">
      <h2>Edit League: {{selectedLeagueOptions.leagueName}} (Year {{selectedLeagueOptions.year}})</h2>
      <hr />
      <form v-if="possibleLeagueOptions" method="post" class="form-horizontal" role="form" v-on:submit.prevent="postRequest">
        <div class="alert alert-danger" v-if="errorInfo">An error has occurred.</div>

        <div class="form-group col-md-10">
          <label for="pickupGames" class="control-label">Total Number of Games</label>
          <input v-model="selectedLeagueOptions.standardGames" id="standardGames" name="standardGames" type="text" class="form-control input" />
        </div>

        <div class="form-group col-md-10">
          <label for="gamesToDraft" class="control-label">Number of Games to Draft</label>
          <input v-model="selectedLeagueOptions.gamesToDraft" id="gamesToDraft" name="gamesToDraft" type="text" class="form-control input" />
        </div>

        <div class="form-group col-md-10">
          <label for="counterPicks" class="control-label">Number of Counter Picks</label>
          <input v-model="selectedLeagueOptions.counterPicks" id="counterPicks" name="counterPicks" type="text" class="form-control input" />
        </div>
        <hr />

        <div class="form-group col-md-10 eligibility-section">
          <label class="control-label eligibility-slider-label">Maximum Eligibility Level</label>
          <vue-slider v-model="selectedLeagueOptions.maximumEligibilityLevel" :min="minimumEligibilityLevel" :max="maximumEligibilityLevel"
                      piecewise piecewise-label :piecewise-style="piecewiseStyle">
          </vue-slider>
          <div>
            <h4>{{ selectedEligibilityLevel.name }}</h4>
            <p>{{ selectedEligibilityLevel.description }}</p>
            <p>Examples: </p>
            <ul>
              <li v-for="example in selectedEligibilityLevel.examples">{{example}}</li>
            </ul>
          </div>
          <div>
            <h4>Other Options</h4>
            <div>
              <b-form-checkbox id="yearly-checkbox"
                               v-model="selectedLeagueOptions.allowYearlyInstallments">
                <span class="checkbox-label">Allow Yearly Installments (IE Yearly Sports Franchises)</span>
              </b-form-checkbox>
            </div>
            <div>
              <b-form-checkbox id="early-access-checkbox"
                               v-model="selectedLeagueOptions.allowEarlyAccess">
                <span class="checkbox-label">Allow Early Access Games</span>
              </b-form-checkbox>
            </div>
          </div>
        </div>

        <div class="form-group">
          <div class="col-md-offset-2 col-md-10">
            <input type="submit" class="btn btn-primary" value="Edit League Settings" />
          </div>
        </div>
      </form>
    </div>
  </div>
</template>
<script>
import Vue from "vue";
import axios from "axios";
import vueSlider from 'vue-slider-component';

export default {
    data() {
      return {
        errorInfo: "",
        possibleLeagueOptions: null,
        selectedLeagueOptions: null,
        piecewiseStyle: {
          "backgroundColor": "#ccc",
          "visibility": "visible",
          "width": "12px",
          "height": "20px"
        }
      }
    },
    props: ['leagueid', 'year'],
    components: {
      vueSlider
    },
    computed: {
      minimumEligibilityLevel() {
        return 0;
      },
      maximumEligibilityLevel() {
        if (!this.possibleLeagueOptions.eligibilityLevels) {
          return 0;
        }
        let maxEligibilityLevel = _.maxBy(this.possibleLeagueOptions.eligibilityLevels, 'level');
        return maxEligibilityLevel.level;
      },
      selectedEligibilityLevel() {
        let matchingLevel = _.filter(this.possibleLeagueOptions.eligibilityLevels, { 'level': this.selectedLeagueOptions.maximumEligibilityLevel });
        return matchingLevel[0];
      }
    },
    methods: {
      fetchLeagueOptions() {
        axios
          .get('/api/League/LeagueOptions')
          .then(response => {
            this.possibleLeagueOptions = response.data;
          })
          .catch(returnedError => (this.error = returnedError));
      },
      fetchCurrentLeagueYearOptions() {
        axios
          .get('/api/League/GetLeagueYearOptions?leagueID=' + this.leagueid + '&year=' + this.year)
          .then(response => {
            this.selectedLeagueOptions = response.data;
          })
          .catch(returnedError => (this.error = returnedError));
      },
      postRequest() {
          axios
            .post('/api/leagueManager/EditLeagueYearSettings', this.selectedLeagueOptions)
            .then(this.responseHandler)
            .catch(this.catchHandler);
      },
      responseHandler(response) {
        this.$router.push({ name: 'league', params: { leagueid: this.leagueid, year: this.year } });
      },
      catchHandler(returnedError) {
        this.errorInfo = returnedError;
      }
    },
    mounted() {
      this.fetchLeagueOptions();
      this.fetchCurrentLeagueYearOptions();
    }
}
</script>
<style scoped>
.eligibility-slider-label {
  margin-bottom: 40px;
}
.eligibility-section {
  margin-bottom: 10px;
}
.checkbox-label {
  padding-left: 25px;
}
</style>
