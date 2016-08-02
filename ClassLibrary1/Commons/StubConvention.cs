using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1.Commons
{
    public enum StubConvention
    {

        /**
         * Explicitly states that there are no stubs.
         * <p>
         * This is used to indicate that the term of the schedule evenly divides by the
         * periodic frequency leaving no stubs.
         * For example, a 6 month trade can be exactly divided by a 3 month frequency.
         * <p>
         * If the term of the schedule is less than the frequency, then only one period exists.
         * In this case, the period is not treated as a stub.
         * <p>
         * When creating a schedule, there must be no explicit stubs.
         */
        NONE,
        /**
         * A short initial stub.
         * <p>
         * The schedule periods will be determined backwards from the end date.
         * Any remaining period, shorter than the standard frequency, will be allocated at the start.
         * <p>
         * For example, an 8 month trade with a 3 month periodic frequency would result in
         * a 2 month initial short stub followed by two periods of 3 months.
         * <p>
         * If there is no remaining period when calculating, then there is no stub.
         * For example, a 6 month trade can be exactly divided by a 3 month frequency.
         * <p>
         * When creating a schedule, there must be no explicit final stub.
         * If there is an explicit initial stub, then this convention is considered to be matched
         * and the remaining period is calculated using the stub convention 'None'.
         */
        SHORT_INITIAL,
        /**
         * A long initial stub.
         * <p>
         * The schedule periods will be determined backwards from the end date.
         * Any remaining period, shorter than the standard frequency, will be allocated at the start
         * and combined with the next period, making a total period longer than the standard frequency.
         * <p>
         * For example, an 8 month trade with a 3 month periodic frequency would result in
         * a 5 month initial long stub followed by one period of 3 months.
         * <p>
         * If there is no remaining period when calculating, then there is no stub.
         * For example, a 6 month trade can be exactly divided by a 3 month frequency.
         * <p>
         * When creating a schedule, there must be no explicit final stub.
         * If there is an explicit initial stub, then this convention is considered to be matched
         * and the remaining period is calculated using the stub convention 'None'.
         */
        LONG_INITIAL,
        /**
         * A short final stub.
         * <p>
         * The schedule periods will be determined forwards from the regular period start date.
         * Any remaining period, shorter than the standard frequency, will be allocated at the end.
         * <p>
         * For example, an 8 month trade with a 3 month periodic frequency would result in
         * two periods of 3 months followed by a 2 month final short stub.
         * <p>
         * If there is no remaining period when calculating, then there is no stub.
         * For example, a 6 month trade can be exactly divided by a 3 month frequency.
         * <p>
         * When creating a schedule, there must be no explicit initial stub.
         * If there is an explicit final stub, then this convention is considered to be matched
         * and the remaining period is calculated using the stub convention 'None'.
         */
        SHORT_FINAL,
        /**
         * A long final stub.
         * <p>
         * The schedule periods will be determined forwards from the regular period start date.
         * Any remaining period, shorter than the standard frequency, will be allocated at the end
         * and combined with the previous period, making a total period longer than the standard frequency.
         * <p>
         * For example, an 8 month trade with a 3 month periodic frequency would result in
         * one period of 3 months followed by a 5 month final long stub.
         * <p>
         * If there is no remaining period when calculating, then there is no stub.
         * For example, a 6 month trade can be exactly divided by a 3 month frequency.
         * <p>
         * When creating a schedule, there must be no explicit initial stub.
         * If there is an explicit final stub, then this convention is considered to be matched
         * and the remaining period is calculated using the stub convention 'None'.
         */
        LONG_FINAL,
        /**
         * Both ends of the schedule have a stub.
         * <p>
         * The schedule periods will be determined from two dates - the regular period start date
         * and the regular period end date.
         * Days before the first regular period start date form the initial stub.
         * Days after the last regular period end date form the final stub.
         * <p>
         * When creating a schedule, there must be both an explicit initial and final stub.
         */
        BOTH
    }
}
