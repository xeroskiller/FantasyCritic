<template>
  <b-modal id="currentBidsForm" ref="currentBidsFormRef" title="Current Bids" hide-footer>
    <table class="table table-sm table-responsive-sm table-bordered">
      <thead>
        <tr class="table-secondary">
          <th scope="col" class="game-column">Game</th>
          <th scope="col">Bid Amount</th>
          <th scope="col">Priority</th>
          <th scope="col">Cancel</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="bid in currentBids">
          <td>{{bid.masterGame.gameName}}</td>
          <td>{{bid.bidAmount | money}}</td>
          <td>{{bid.priority}}</td>
          <td>
            <b-button variant="danger" size="sm" v-on:click="cancelBid(bid)">Cancel</b-button>
          </td>
        </tr>
      </tbody>
    </table>
  </b-modal>
</template>

<script>
    import Vue from "vue";
    import axios from "axios";
    export default {
        props: ['currentBids'],
        methods: {
          cancelBid(bid) {
            var model = {
              bidID: bid.bidID
            };
            axios
              .post('/api/league/DeletePickupBid', model)
              .then(response => {
                var bidInfo = {
                  gameName: bid.masterGame.gameName,
                  bidAmount: bid.bidAmount
                };
                this.$emit('bidCanceled', bidInfo);
              })
              .catch(response => {

              });
          }
        }
    }
</script>
<style scoped>

</style>
