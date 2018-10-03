<template>
  <div class="home">
    <el-row type="flex" justify="center" class="card-steps">
      <el-col :span="16">
        <el-tabs type="border-card">
          <el-tab-pane label="Random Test">
            <el-form :label-position="'top'" label-width="100px" :model="formLabelAlign" class="form-initial-configs">
              <el-form-item :label="'File Name'">
                <el-input placeholder="Please input the file name" v-model="testFileName"></el-input>
              </el-form-item>
            </el-form>
            <el-button type="success" @click="createTest">Random Test</el-button>
            <el-button type="success" @click="callTest">Call Test</el-button>
            <div v-for="dayAllocation in daysAllocation" :key="dayAllocation.id" style="padding-top: 20px">
              <el-tag type="info">Day {{ dayAllocation.id }}</el-tag>
              <el-tag style="margin-left: 10px" type="info">Func. Obj {{ funcObj }}</el-tag>
              <el-tag style="margin-left: 10px" type="info">Groups Splits {{ dayAllocation.numGroupsSplits }}</el-tag>
              <result :roomAllocation="dayAllocation.roomAllocation" :funcObj="funcObj"/>
            </div>
          </el-tab-pane>
          <el-tab-pane label="Input">
            <el-card>
              <div slot="header" class="clearfix card-header">
                <h1>Hostel Allocation Optimizer</h1>
              </div>
              <el-steps :active="active" finish-status="success">
                <el-step title="Initial Config.">
                </el-step>
                <el-step title="Groups"></el-step>
                <el-step title="Initial Allocations"></el-step>
                <el-step title="Result"></el-step>
              </el-steps>

              <el-row type="flex" justify="center" v-if="this.active == 0">
                <el-form :label-position="labelPosition" label-width="100px" :model="formLabelAlign" class="form-initial-configs">
                  <el-form-item label="Period">
                    <el-date-picker
                      v-model="period"
                      type="daterange"
                      start-placeholder="Start date"
                      end-placeholder="End date">
                    </el-date-picker>
                  </el-form-item>
                  <el-form-item label="Groups">
                    <el-input-number v-model="numGroups" @change="changeNumGroups" :min="0"></el-input-number>
                  </el-form-item>
                  <div v-if="this.groupsSizes.length && this.groupsSizes.length > 0">
                    <el-form-item :label="'Group ' + group  + ' Size'" v-for="group in groupsArr" :key="group">
                      <el-input-number v-model="groupsSizes[group]"></el-input-number>
                    </el-form-item>
                  </div>
                  <el-form-item label="Rooms">
                    <el-input-number v-model="numRooms" @change="changeNumRooms" :min="0"></el-input-number>
                  </el-form-item>

                  <div v-if="this.roomsSizes.length && this.roomsSizes.length > 0">
                    <el-form-item :label="'Room ' + room  + ' Size'" v-for="room in roomsArr" :key="room">
                      <el-input-number v-model="roomsSizes[room]"></el-input-number>
                    </el-form-item>
                  </div>

                  <el-button type="success" @click="next">Next</el-button>
                </el-form>
              </el-row>

              <el-row type="flex" justify="center" v-if="this.active == 1">
                <el-form :label-position="'top'" label-width="100px" :model="formLabelAlign" class="form-initial-configs">
                  <el-form-item :label="'Group ' + group.id  + ' Demand'" v-for="group in groupsAllocation" :key="group.id">
                    <el-date-picker
                      v-model="group.range"
                      type="daterange"
                      start-placeholder="Start date"
                      end-placeholder="End date">
                    </el-date-picker>
                  </el-form-item>
                  <el-button type="success" @click="back">Back</el-button>
                  <el-button type="success" @click="next">Next</el-button>
                </el-form>
              </el-row>

              <el-row type="flex" justify="center" v-if="this.active == 2">
                <el-form v-if="this.initialAllocation.length" :label-position="'top'" label-width="100px" :model="formLabelAlign" class="form-initial-configs">
                  <el-form-item :label="'Group ' + group.idGroup  + ' room'" v-for="group in initialAllocation" :key="group.id">
                    <el-input-number v-model="group.idRoom"></el-input-number>
                  </el-form-item>
                  <el-button type="success" @click="back">Back</el-button>
                    <el-button type="success" @click="next">Next</el-button>
                </el-form>
                <el-form v-else :label-position="'top'" label-width="100px" :model="formLabelAlign" class="form-initial-configs">
                  <p>No groups were allocated on the first day.</p>
                  <el-button type="success" @click="back">Back</el-button>
                  <el-button type="success" @click="next">Next</el-button>
                </el-form>
              </el-row>
            </el-card>
          </el-tab-pane>
        </el-tabs>
      </el-col>
    </el-row>
  </div>
</template>

<script scoped>
import axios from 'axios'
import moment from 'moment'
import Result from './Result.vue'
export default {
  name: 'home',
  components: {
    Result: Result
  },
  data () {
    return {
      daysAllocation: null,
      testFileName: null,
      period: null,
      active: 0,
      labelPosition: 'left',
      formLabelAlign: {
        name: '',
        region: '',
        type: ''
      },
      numDays: 0,
      numGroups: 0,
      numRooms: 0,
      roomsArr: [],
      roomsSizes: [],
      groupsArr: [],
      groupsSizes: [],
      groupsAllocation: [],
      initialAllocation: [],
      funcObj: []
    }
  },
  methods: {
    next () {
      if (this.active === 0) {
        this.processFirstStep()
      } else if (this.active === 1) {
        this.processSecondStep()
      } else if (this.active === 2) {
        this.processThirdStep()
      } else if (this.active < 3) {
        this.active = 0
      }
    },
    back () {
      if (this.active > 0) {
        this.active--
      }
    },
    changeNumRooms (newNumRooms) {
      const actualNumRooms = this.roomsSizes.length
      if (newNumRooms >= actualNumRooms) {
        for (let i = 0; i < newNumRooms - actualNumRooms; i++) {
          this.roomsSizes[actualNumRooms + i] = 0
        }
      } else {
        this.roomsSizes.splice(0, newNumRooms)
      }
      this.roomsArr = Array.from(Array(newNumRooms).keys())
    },
    changeNumGroups (newNumGroups) {
      const actualNumGroups = this.groupsSizes.length
      if (newNumGroups >= actualNumGroups) {
        for (let i = 0; i < newNumGroups - actualNumGroups; i++) {
          this.groupsSizes[actualNumGroups + i] = 0
        }
      } else {
        this.groupsSizes.splice(0, newNumGroups)
      }
      this.groupsArr = Array.from(Array(newNumGroups).keys())
    },
    processFirstStep () {
      if (this.validateFirstStep()) {
        this.groupsAllocation = []
        for (let i = 0; i < this.numGroups; i++) {
          this.groupsAllocation.push({
            range: null,
            id: this.groupsAllocation.length
          })
        }
        this.active++
      } else {
        this.$notify({
          title: 'Invalid Registration',
          message: 'Please select a valid registration'
        })
      }
    },
    validateFirstStep () {
      return this.period && this.numGroups > 0 && this.numRooms > 0
    },
    processSecondStep () {
      if (this.validateSecondStep()) {
        this.createGroupDemands()
        this.initialAllocation = []
        let groupsAllocatedFirstDay = this.groupsAllocation.filter(g => moment(g.range[0]).isSame(this.period[0]))
        if (groupsAllocatedFirstDay !== null && groupsAllocatedFirstDay.length) {
          for (let i = 0; i < groupsAllocatedFirstDay.length; i++) {
            this.initialAllocation.push({
              idGroup: groupsAllocatedFirstDay[i].id,
              idRoom: null
            })
          }
        }
        this.active++
      } else {
        this.$notify({
          title: 'Invalid Allocation',
          message: 'Please select a valid allocation for every group'
        })
      }
    },
    validateSecondStep () {
      return this.groupsAllocation.map(g => g.range)
        .every(r => r !== null && moment(r[0]).isSameOrAfter(this.period[0]) && moment(r[1]).isSameOrBefore(this.period[1]))
    },
    processThirdStep () {
      if (this.validateThirdStep()) {
        this.processParameters()
      } else {
        this.$notify({
          title: 'Invalid Room',
          message: 'Please select a valid room for each group'
        })
      }
    },
    validateThirdStep () {
      return this.initialAllocation.length === 0 || this.initialAllocation.map(i => i.idRoom).every(r => r !== null && r < this.numRooms)
    },
    processParameters () {
      let startDay = moment(this.period[0])
      let lastDay = moment(this.period[1])
      this.numDays = lastDay.diff(startDay, 'days') + 1
      let groupsDemands = []
      this.groupsAllocation.forEach(g => {
        g.demand.forEach(d => groupsDemands.push(d))
      })
      let hostelAllocation = {
        numDays: this.numDays,
        numRooms: this.numRooms,
        roomCapacity: this.roomsSizes,
        initialAllocation: this.initialAllocation.map(t => { return { Item1: t.idGroup, Item2: t.idRoom } }),
        groupsDemands: groupsDemands,
        groupsSizes: this.groupsSizes
      }
      axios.post('http://localhost:52904/api/optimizer', hostelAllocation)
        .then(response => {
          console.log('response')
          this.funcObj = response.data[0].funcObj
        })
        .catch(error => console.log(error))
    },
    createGroupDemands () {
      this.groupsAllocation.forEach(g => {
        g.demand = []
        let startDay = moment(g.range[0])
        let lastDay = moment(g.range[1])
        let numDays = lastDay.diff(startDay, 'days') + 1
        for (let i in Array.from(Array(numDays).keys())) {
          g.demand.push({
            Item1: g.id,
            Item2: i
          })
        }
      })
    },
    createTest () {
      if (!this.testFileName) {
        this.$notify({
          title: 'Invalid File Test name',
          message: 'Invalid File Test name'
        })
      } else {
        axios.get('http://localhost:52904/api/TestFactory?fileName=' + this.testFileName)
          .then(response => {
            console.log('response')
            console.log(response.data)
            this.daysAllocation = response.data.map((r, index) => {
              return {
                roomAllocation: r.roomAllocation,
                id: index
              }
            })
            console.log(this.daysAllocation)
          })
          .catch(error => console.log(error))
      }
    },
    callTest () {
      if (!this.testFileName) {
        this.$notify({
          title: 'Invalid File Test name',
          message: 'Invalid File Test name'
        })
      } else {
        axios.get('http://localhost:52904/api/Optimizer?jsonFile=' + this.testFileName)
          .then(response => {
            this.funcObj = response.data[0].funcObj
            this.daysAllocation = response.data.map((r, index) => {
              return {
                numGroupsSplits: r.numGroupsSplits,
                roomAllocation: r.roomAllocation,
                id: index
              }
            })
            console.log(this.daysAllocation)
          })
          .catch(error => {
            this.$notify({
              title: 'Error',
              message: error.message
            })
          })
      }
    }
  }
}
</script>

<style>
  h1 {
    font-family: 'Source Sans Pro', sans-serif;
    text-align: center;
  }

  .card-steps .el-card__header {
    background-color: #9d261b;
    color: #e7e7e7;
    text-transform: uppercase;
  }

  .home .el-card {
    border: 0px;
  }

  .card-steps {
    padding-top: 5%;
  }

  .form-initial-configs {
    margin-top: 30px;
    margin-bottom: 30px;
  }
</style>
