export interface ResGetVtiDto {
    sn: string
    stockId: string
    stockName: string
    price: number
    highIn52: number
    lowIn52: number
    epsInterval: string
    eps: number
    pe: number
    revenueDatas: RevenueData[]
    volumeDatas: VolumeData[]
    vti: number
    amount: number
  }
  
  export interface RevenueData {
    revenueInterval: string
    mom: number
    monthYOY: number
    yoy: number
  }
  
  export interface VolumeData {
    txDate: string
    foreignSellVolK: number
    dealerDiffVolK: number
    investmentTrustDiffVolK: number
    volumeK: number
  }