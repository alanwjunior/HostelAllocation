<template>
  <div style="padding-top: 20px">
    <el-row :gutter="20">
      <el-col :span="4" v-for="(room, index) in roomAllocation" :key="index">
        <el-card class="box-card">
          <div slot="header" class="clearfix" style="font-size: 10px; font-weight: 700;">
            Room {{ index }}
          </div>
          <div class="text item">
            Groups: {{ room }}
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script>
export default {
  props: {
    roomAllocation: {
      required: true,
      type: Object
    },
    funcObj: {
      required: true,
      type: Number
    }
  }
}
</script>
